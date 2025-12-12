// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Extensions.VectorData.ProviderServices;
using Microsoft.Extensions.VectorData.ProviderServices.Filter;

namespace Microsoft.SemanticKernel.Connectors;

#pragma warning disable MEVD9001 // Microsoft.Extensions.VectorData experimental connector-facing APIs


internal abstract class SqlFilterTranslator
{
    private readonly CollectionModel _model;
    private readonly LambdaExpression _lambdaExpression;
    private readonly ParameterExpression _recordParameter;
    protected readonly StringBuilder _sql;


    internal SqlFilterTranslator(
        CollectionModel model,
        LambdaExpression lambdaExpression,
        StringBuilder? sql = null)
    {
        _model = model;
        _lambdaExpression = lambdaExpression;
        Debug.Assert(lambdaExpression.Parameters.Count == 1);
        _recordParameter = lambdaExpression.Parameters[0];
        _sql = sql ?? new StringBuilder();
    }


    internal StringBuilder Clause => _sql;


    internal void Translate(bool appendWhere)
    {
        if (appendWhere)
        {
            _sql.Append("WHERE ");
        }

        var preprocessor = new FilterTranslationPreprocessor { SupportsParameterization = true };
        var preprocessedExpression = preprocessor.Preprocess(_lambdaExpression.Body);

        Translate(preprocessedExpression, true);
    }


    protected void Translate(Expression? node, bool isSearchCondition = false)
    {
        switch (node)
        {
            case BinaryExpression binary:
                TranslateBinary(binary);
                return;

            case ConstantExpression constant:
                TranslateConstant(constant.Value);
                return;

            case QueryParameterExpression { Name: var name, Value: var value }:
                TranslateQueryParameter(value);
                return;

            case MemberExpression member:
                TranslateMember(member, isSearchCondition);
                return;

            case MethodCallExpression methodCall:
                TranslateMethodCall(methodCall, isSearchCondition);
                return;

            case UnaryExpression unary:
                TranslateUnary(unary, isSearchCondition);
                return;

            default:
                throw new NotSupportedException("Unsupported NodeType in filter: " + node?.NodeType);
        }
    }


    protected void TranslateBinary(BinaryExpression binary)
    {
        // Special handling for null comparisons
        switch (binary.NodeType)
        {
            case ExpressionType.Equal when IsNull(binary.Right):
                _sql.Append('(');
                Translate(binary.Left);
                _sql.Append(" IS NULL)");
                return;
            case ExpressionType.NotEqual when IsNull(binary.Right):
                _sql.Append('(');
                Translate(binary.Left);
                _sql.Append(" IS NOT NULL)");
                return;

            case ExpressionType.Equal when IsNull(binary.Left):
                _sql.Append('(');
                Translate(binary.Right);
                _sql.Append(" IS NULL)");
                return;
            case ExpressionType.NotEqual when IsNull(binary.Left):
                _sql.Append('(');
                Translate(binary.Right);
                _sql.Append(" IS NOT NULL)");
                return;
        }

        _sql.Append('(');
        Translate(binary.Left, binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse);

        _sql.Append(binary.NodeType switch
        {
            ExpressionType.Equal => " = ",
            ExpressionType.NotEqual => " <> ",

            ExpressionType.GreaterThan => " > ",
            ExpressionType.GreaterThanOrEqual => " >= ",
            ExpressionType.LessThan => " < ",
            ExpressionType.LessThanOrEqual => " <= ",

            ExpressionType.AndAlso => " AND ",
            ExpressionType.OrElse => " OR ",

            _ => throw new NotSupportedException("Unsupported binary expression node type: " + binary.NodeType)
        });

        Translate(binary.Right, binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse);

        _sql.Append(')');

        static bool IsNull(Expression expression)
        {
            return expression is ConstantExpression { Value: null } or QueryParameterExpression { Value: null };
        }
    }


    protected virtual void TranslateConstant(object? value)
    {
        switch (value)
        {
            case byte b:
                _sql.Append(b);
                return;
            case short s:
                _sql.Append(s);
                return;
            case int i:
                _sql.Append(i);
                return;
            case long l:
                _sql.Append(l);
                return;

            case float f:
                _sql.Append(f);
                return;
            case double d:
                _sql.Append(d);
                return;
            case decimal d:
                _sql.Append(d);
                return;

            case string untrustedInput:
                // This is the only place where we allow untrusted input to be passed in, so we need to quote and escape it.
                // Luckily for us, values are escaped in the same way for every provider that we support so far.
                _sql.Append('\'').Append(untrustedInput.Replace("'", "''")).Append('\'');
                return;
            case bool b:
                _sql.Append(b
                    ? "TRUE"
                    : "FALSE");
                return;
            case Guid g:
                _sql.Append('\'').Append(g.ToString()).Append('\'');
                return;

            case DateTime dateTime:
            case DateTimeOffset dateTimeOffset:
            case Array:
#if NET
            case DateOnly dateOnly:
            case TimeOnly timeOnly:
#endif
                throw new UnreachableException("Database-specific format, needs to be implemented in the provider's derived translator.");

            case null:
                _sql.Append("NULL");
                return;

            default:
                throw new NotSupportedException("Unsupported constant type: " + value.GetType().Name);
        }
    }


    private void TranslateMember(MemberExpression memberExpression, bool isSearchCondition)
    {
        if (TryBindProperty(memberExpression, out var property))
        {
            GenerateColumn(property, isSearchCondition);
            return;
        }

        throw new NotSupportedException($"Member access for '{memberExpression.Member.Name}' is unsupported - only member access over the filter parameter are supported");
    }


    protected virtual void GenerateColumn(PropertyModel property, bool isSearchCondition = false) // StorageName is considered to be a safe input, we quote and escape it mostly to produce valid SQL.
    {
    _sql.Append('"').Append(property.StorageName.Replace("\"", "\"\"")).Append('"');
    }


    protected abstract void TranslateQueryParameter(object? value);


    private void TranslateMethodCall(MethodCallExpression methodCall, bool isSearchCondition = false)
    {
        switch (methodCall)
        {
            // Dictionary access for dynamic mapping (r => r["SomeString"] == "foo")
            case MethodCallExpression when TryBindProperty(methodCall, out var property):
                GenerateColumn(property, isSearchCondition);
                return;

            // Enumerable.Contains()
            case { Method.Name: nameof(Enumerable.Contains), Arguments: [var source, var item] } contains
                when contains.Method.DeclaringType == typeof(Enumerable):
                TranslateContains(source, item);
                return;

            // List.Contains()
            case
            {
                Method:
                {
                    Name: nameof(Enumerable.Contains),
                    DeclaringType: { IsGenericType: true } declaringType
                },
                Object: Expression source,
                Arguments: [var item]
            } when declaringType.GetGenericTypeDefinition() == typeof(List<>):
                TranslateContains(source, item);
                return;

            default:
                throw new NotSupportedException($"Unsupported method call: {methodCall.Method.DeclaringType?.Name}.{methodCall.Method.Name}");
        }

        static bool TryUnwrapSpanImplicitCast(Expression expression, [NotNullWhen(true)] out Expression? result)
        {
            // Different versions of the compiler seem to generate slightly different expression tree representations for this
            // implicit cast:
            var (unwrapped, castDeclaringType) = expression switch
            {
                UnaryExpression
                {
                    NodeType: ExpressionType.Convert,
                    Method: { Name: "op_Implicit", DeclaringType: { IsGenericType: true } implicitCastDeclaringType },
                    Operand: var operand
                } => (operand, implicitCastDeclaringType),

                MethodCallExpression
                {
                    Method: { Name: "op_Implicit", DeclaringType: { IsGenericType: true } implicitCastDeclaringType },
                    Arguments: [var firstArgument]
                } => (firstArgument, implicitCastDeclaringType),

                _ => (null, null)
            };

            // For the dynamic case, there's a Convert node representing an up-cast to object[]; unwrap that too.
            if (unwrapped is UnaryExpression
                {
                    NodeType: ExpressionType.Convert,
                    Method: null
                } convert
                && convert.Type == typeof(object[]))
            {
                result = convert.Operand;
                return true;
            }

            if (unwrapped is not null
                && castDeclaringType?.GetGenericTypeDefinition() is var genericTypeDefinition
                    && (genericTypeDefinition == typeof(Span<>) || genericTypeDefinition == typeof(ReadOnlySpan<>)))
            {
                result = unwrapped;
                return true;
    }


    private void TranslateContains(Expression source, Expression item)
    {
        switch (source)
        {
            // Contains over array column (r => r.Strings.Contains("foo"))
            case var _ when TryBindProperty(source, out _):
                TranslateContainsOverArrayColumn(source, item);
                return;

            // Contains over inline array (r => new[] { "foo", "bar" }.Contains(r.String))
            case NewArrayExpression newArray:
                Translate(item);
                _sql.Append(" IN (");

                var isFirst = true;

                foreach (var element in newArray.Expressions)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        _sql.Append(", ");
                    }

                    Translate(element);
                }

                _sql.Append(')');
                return;

            // Contains over captured array (r => arrayLocalVariable.Contains(r.String))
            case QueryParameterExpression { Value: var value }:
                TranslateContainsOverParameterizedArray(source, item, value);
                return;

            default:
                throw new NotSupportedException("Unsupported Contains expression");
        }
    }


    protected abstract void TranslateContainsOverArrayColumn(Expression source, Expression item);

    protected abstract void TranslateContainsOverParameterizedArray(Expression source, Expression item, object? value);


    private void TranslateUnary(UnaryExpression unary, bool isSearchCondition)
    {
        switch (unary.NodeType)
        {
            case ExpressionType.Not:
                // Special handling for !(a == b) and !(a != b)
                if (unary.Operand is BinaryExpression { NodeType: ExpressionType.Equal or ExpressionType.NotEqual } binary)
                {
                    TranslateBinary(
                        Expression.MakeBinary(
                            binary.NodeType is ExpressionType.Equal
                                ? ExpressionType.NotEqual
                                : ExpressionType.Equal,
                            binary.Left,
                            binary.Right));
                    return;
                }

                _sql.Append("(NOT ");
                Translate(unary.Operand, isSearchCondition);
                _sql.Append(')');
                return;

            // Handle converting non-nullable to nullable; such nodes are found in e.g. r => r.Int == nullableInt
            case ExpressionType.Convert when Nullable.GetUnderlyingType(unary.Type) == unary.Operand.Type:
                Translate(unary.Operand, isSearchCondition);
                return;

            // Handle convert over member access, for dynamic dictionary access (r => (int)r["SomeInt"] == 8)
            case ExpressionType.Convert when TryBindProperty(unary.Operand, out var property) && unary.Type == property.Type:
                GenerateColumn(property, isSearchCondition);
                return;

            default:
                throw new NotSupportedException("Unsupported unary expression node type: " + unary.NodeType);
        }
    }


    private bool TryBindProperty(Expression expression, [NotNullWhen(true)] out PropertyModel? property)
    {
        var unwrappedExpression = expression;

        while (unwrappedExpression is UnaryExpression { NodeType: ExpressionType.Convert } convert)
        {
            unwrappedExpression = convert.Operand;
        }

        var modelName = unwrappedExpression switch
        {
            // Regular member access for strongly-typed POCO binding (e.g. r => r.SomeInt == 8)
            MemberExpression memberExpression when memberExpression.Expression == _recordParameter
                => memberExpression.Member.Name,

            // Dictionary lookup for weakly-typed dynamic binding (e.g. r => r["SomeInt"] == 8)
            MethodCallExpression
                {
                    Method: { Name: "get_Item", DeclaringType: var declaringType },
                    Arguments: [ConstantExpression { Value: string keyName }]
                } methodCall when methodCall.Object == _recordParameter && declaringType == typeof(Dictionary<string, object?>)
                => keyName,

            _ => null
        };

        if (modelName is null)
        {
            property = null;
            return false;
        }

        if (!_model.PropertyMap.TryGetValue(modelName, out property))
        {
            throw new InvalidOperationException($"Property name '{modelName}' provided as part of the filter clause is not a valid property name.");
        }

        // Now that we have the property, go over all wrapping Convert nodes again to ensure that they're compatible with the property type
        var unwrappedPropertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;
        unwrappedExpression = expression;

        while (unwrappedExpression is UnaryExpression { NodeType: ExpressionType.Convert } convert)
        {
            var convertType = Nullable.GetUnderlyingType(convert.Type) ?? convert.Type;

            if (convertType != unwrappedPropertyType && convertType != typeof(object))
            {
                throw new InvalidCastException($"Property '{property.ModelName}' is being cast to type '{convert.Type.Name}', but its configured type is '{property.Type.Name}'.");
            }

            unwrappedExpression = convert.Operand;
        }

        return true;
    }
}
