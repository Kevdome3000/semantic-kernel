using Microsoft.Extensions.VectorData.ProviderServices.Filter;
using Microsoft.Extensions.VectorData.ProviderServices;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Linq;
using System.Text;
using System;
﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.CosmosNoSql;

#pragma warning disable MEVD9001 // Experimental: filter translation base types


internal class CosmosNoSqlFilterTranslator : FilterTranslatorBase
{
    private readonly Dictionary<string, object?> _parameters = [];
    private readonly StringBuilder _sql = new();


    internal (string WhereClause, Dictionary<string, object?> Parameters) Translate(LambdaExpression lambdaExpression, CollectionModel model)
    {
        var preprocessedExpression = this.PreprocessFilter(lambdaExpression, model, new FilterPreprocessingOptions { SupportsParameterization = true });

        Translate(preprocessedExpression);

        return (_sql.ToString(), _parameters);
    }


    private void Translate(Expression? node)
    {
        switch (node)
        {
            case BinaryExpression binary:
                TranslateBinary(binary);
                return;

            case ConstantExpression constant:
                TranslateConstant(constant);
                return;

            case QueryParameterExpression { Name: var name, Value: var value }:
                TranslateQueryParameter(name, value);
                return;

            case MemberExpression member:
                TranslateMember(member);
                return;

            case NewArrayExpression newArray:
                TranslateNewArray(newArray);
                return;

            case MethodCallExpression methodCall:
                TranslateMethodCall(methodCall);
                return;

            case UnaryExpression unary:
                TranslateUnary(unary);
                return;

            default:
                throw new NotSupportedException("Unsupported NodeType in filter: " + node?.NodeType);
        }
    }


    private void TranslateBinary(BinaryExpression binary)
    {
        _sql.Append('(');
        Translate(binary.Left);

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

        Translate(binary.Right);
        _sql.Append(')');
    }


    private void TranslateConstant(ConstantExpression constant)
    {
        this.TranslateConstant(constant.Value);
    }


    private void TranslateConstant(object? value)
    {
        switch (value)
        {
            case byte v:
                _sql.Append(v);
                return;
            case short v:
                _sql.Append(v);
                return;
            case int v:
                _sql.Append(v);
                return;
            case long v:
                _sql.Append(v);
                return;

            case float v:
                _sql.Append(v);
                return;
            case double v:
                _sql.Append(v);
                return;

            case string v:
                _sql.Append('"').Append(v.Replace(@"\", @"\\").Replace("\"", "\\\"")).Append('"');
                return;
            case bool v:
                _sql.Append(v
                    ? "true"
                    : "false");
                return;
            case Guid v:
                _sql.Append('"').Append(v.ToString()).Append('"');
                return;

            case DateTimeOffset v:
                // Cosmos doesn't support DateTimeOffset with non-zero offset, so we convert it to UTC.
                // See https://github.com/dotnet/efcore/issues/35310
                _sql
                    .Append('"')
                    .Append(v.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFFFF", CultureInfo.InvariantCulture))
                    .Append("Z\"");
                return;

            case DateTime v:
                _sql
                    .Append('"')
                    .Append(v.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFF", CultureInfo.InvariantCulture))
                    .Append('"');
                return;

#if NET
            case DateOnly v:
                this._sql
                    .Append('"')
                    .Append(v.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                    .Append('"');
                return;
#endif

            case IEnumerable v when v.GetType() is var type && (type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)):
                _sql.Append('[');

                var i = 0;

                foreach (var element in v)
                {
                    if (i++ > 0)
                    {
                        _sql.Append(',');
                    }

                    this.TranslateConstant(element);
                }

                _sql.Append(']');
                return;

            case null:
                _sql.Append("null");
                return;

            default:
                throw new NotSupportedException("Unsupported constant type: " + value.GetType().Name);
        }
    }


    private void TranslateMember(MemberExpression memberExpression)
    {
        if (this.TryBindProperty(memberExpression, out var property))
        {
            GeneratePropertyAccess(property);
            return;
        }

        throw new NotSupportedException($"Member access for '{memberExpression.Member.Name}' is unsupported - only member access over the filter parameter are supported");
    }


    private void TranslateNewArray(NewArrayExpression newArray)
    {
        _sql.Append('[');

        for (var i = 0; i < newArray.Expressions.Count; i++)
        {
            if (i > 0)
            {
                _sql.Append(", ");
            }

            Translate(newArray.Expressions[i]);
        }

        _sql.Append(']');
    }


    private void TranslateMethodCall(MethodCallExpression methodCall)
    {
        // Dictionary access for dynamic mapping (r => r["SomeString"] == "foo")
        if (this.TryBindProperty(methodCall, out var property))
        {
            GeneratePropertyAccess(property);
            return;
        }

        switch (methodCall)
        {
            // Enumerable.Contains(), List.Contains(), MemoryExtensions.Contains()
            case var _ when TryMatchContains(methodCall, out var source, out var item):
                TranslateContains(source, item);
                return;

            // Enumerable.Any() with a Contains predicate (r => r.Strings.Any(s => array.Contains(s)))
            case { Method.Name: nameof(Enumerable.Any), Arguments: [var anySource, LambdaExpression lambda] } any
                when any.Method.DeclaringType == typeof(Enumerable):
                TranslateAny(anySource, lambda);
                return;

            default:
                throw new NotSupportedException($"Unsupported method call: {methodCall.Method.DeclaringType?.Name}.{methodCall.Method.Name}");
        }
    }


    private void TranslateContains(Expression source, Expression item)
    {
        _sql.Append("ARRAY_CONTAINS(");
        Translate(source);
        _sql.Append(", ");
        Translate(item);
        _sql.Append(')');
    }


    /// <summary>
    /// Translates an Any() call with a Contains predicate, e.g. r.Strings.Any(s => array.Contains(s)).
    /// This checks whether any element in the array field is contained in the given values.
    /// </summary>
    private void TranslateAny(Expression source, LambdaExpression lambda)
    {
        // We only support the pattern: r.ArrayField.Any(x => values.Contains(x))
        // Translates to: EXISTS(SELECT VALUE t FROM t IN c["Field"] WHERE ARRAY_CONTAINS(@values, t))
        if (!this.TryBindProperty(source, out var property)
            || lambda.Body is not MethodCallExpression containsCall
            || !TryMatchContains(containsCall, out var valuesExpression, out var itemExpression))
        {
            throw new NotSupportedException("Unsupported method call: Enumerable.Any");
        }

        // Verify that the item is the lambda parameter
        if (itemExpression != lambda.Parameters[0])
        {
            throw new NotSupportedException("Unsupported method call: Enumerable.Any");
        }

        // Now extract the values from valuesExpression
        // Generate: EXISTS(SELECT VALUE t FROM t IN c["Field"] WHERE ARRAY_CONTAINS(@values, t))
        switch (valuesExpression)
        {
            // Inline array: r.Strings.Any(s => new[] { "a", "b" }.Contains(s))
            case NewArrayExpression newArray:
            {
                var values = new object?[newArray.Expressions.Count];

                for (var i = 0; i < newArray.Expressions.Count; i++)
                {
                    values[i] = newArray.Expressions[i] switch
                    {
                        ConstantExpression { Value: var v } => v,
                        QueryParameterExpression { Value: var v } => v,
                        _ => throw new NotSupportedException("Unsupported method call: Enumerable.Any")
                    };
                }

                this.GenerateAnyContains(property, values);
                return;
            }

            // Captured/parameterized array or list: r.Strings.Any(s => capturedArray.Contains(s))
            case QueryParameterExpression queryParameter:
                GenerateAnyContains(property, queryParameter);
                return;

            // Constant array: shouldn't normally happen, but handle it
            case ConstantExpression { Value: var value }:
                this.GenerateAnyContains(property, value);
                return;

            default:
                throw new NotSupportedException("Unsupported method call: Enumerable.Any");
        }
    }


    private void GenerateAnyContains(PropertyModel property, object? values)
    {
        _sql.Append("EXISTS(SELECT VALUE t FROM t IN ");
        GeneratePropertyAccess(property);
        _sql.Append(" WHERE ARRAY_CONTAINS(");
        TranslateConstant(values);
        _sql.Append(", t))");
    }


    private void GenerateAnyContains(PropertyModel property, QueryParameterExpression queryParameter)
    {
        _sql.Append("EXISTS(SELECT VALUE t FROM t IN ");
        GeneratePropertyAccess(property);
        _sql.Append(" WHERE ARRAY_CONTAINS(");
        TranslateQueryParameter(queryParameter.Name, queryParameter.Value);
        _sql.Append(", t))");
    }


    private void TranslateUnary(UnaryExpression unary)
    {
        switch (unary.NodeType)
        {
            // Special handling for !(a == b) and !(a != b)
            case ExpressionType.Not:
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
                Translate(unary.Operand);
                _sql.Append(')');
                return;

            // Handle converting non-nullable to nullable; such nodes are found in e.g. r => r.Int == nullableInt
            case ExpressionType.Convert when Nullable.GetUnderlyingType(unary.Type) == unary.Operand.Type:
                Translate(unary.Operand);
                return;

            // Handle convert over member access, for dynamic dictionary access (r => (int)r["SomeInt"] == 8)
            case ExpressionType.Convert when this.TryBindProperty(unary.Operand, out var property) && unary.Type == property.Type:
                GeneratePropertyAccess(property);
                return;

            default:
                throw new NotSupportedException("Unsupported unary expression node type: " + unary.NodeType);
        }
    }


    protected void TranslateQueryParameter(string name, object? value)
    {
        name = '@' + name;
        _parameters.Add(name, value);
        _sql.Append(name);
    }


    protected virtual void GeneratePropertyAccess(PropertyModel property)
    {
        _sql
            .Append(CosmosNoSqlConstants.ContainerAlias)
            .Append("[\"")
            .Append(property.StorageName.Replace(@"\", @"\\").Replace("\"", "\\\""))
            .Append("\"]");
    }
}
