// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Extensions.VectorData.ProviderServices;

namespace Microsoft.SemanticKernel.Connectors.PgVector;

internal sealed class PostgresFilterTranslator : SqlFilterTranslator
{
    private int _parameterIndex;


    internal PostgresFilterTranslator(
        CollectionModel model,
        LambdaExpression lambdaExpression,
        int startParamIndex,
        StringBuilder? sql = null) : base(model, lambdaExpression, sql)
    {
        _parameterIndex = startParamIndex;
    }


    internal List<object> ParameterValues { get; } = [];


    protected override void TranslateConstant(object? value, bool isSearchCondition)
    {
        switch (value)
        {
            case DateTime dateTime:
                switch (dateTime.Kind)
                {
                    case DateTimeKind.Utc:
                        _sql.Append('\'').Append(dateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFZ", CultureInfo.InvariantCulture)).Append('\'');
                        return;

                    case DateTimeKind.Unspecified:
                    case DateTimeKind.Local:
                        _sql.Append('\'').Append(dateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFF", CultureInfo.InvariantCulture)).Append('\'');
                        return;

                    default:
                        throw new UnreachableException();
                }

            case DateTimeOffset dateTimeOffset:
                if (dateTimeOffset.Offset != TimeSpan.Zero)
                {
                    throw new NotSupportedException("DateTimeOffset with non-zero offset is not supported with PostgreSQL. Use DateTimeOffset.UtcNow or convert to UTC.");
                }

                _sql.Append('\'').Append(dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFZ", CultureInfo.InvariantCulture)).Append('\'');
                return;

#if NET
            case DateOnly dateOnly:
                _sql.Append('\'').Append(dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('\'');
                return;

            case TimeOnly timeOnly:
                _sql.Append('\'').Append(timeOnly.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture)).Append('\'');
                return;
#endif

            // Array constants (ARRAY[1, 2, 3])
            case IEnumerable v when v.GetType() is var type && (type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)):
                _sql.Append("ARRAY[");

                var i = 0;

                foreach (var element in v)
                {
                    if (i++ > 0)
                    {
                        _sql.Append(',');
                    }

                    TranslateConstant(element, false);
                }

                _sql.Append(']');
                return;

            default:
                base.TranslateConstant(value, isSearchCondition);
                break;
        }
    }


    protected override void TranslateContainsOverArrayColumn(Expression source, Expression item)
    {
        Translate(source);
        _sql.Append(" @> ARRAY[");
        Translate(item);
        _sql.Append(']');
    }


    protected override void TranslateContainsOverParameterizedArray(Expression source, Expression item, object? value)
    {
        Translate(item);
        _sql.Append(" = ANY (");
        Translate(source);
        _sql.Append(')');
    }


    protected override void TranslateAnyContainsOverArrayColumn(PropertyModel property, object? values)
    {
        // Translate r.Strings.Any(s => array.Contains(s)) to: column && ARRAY[values]
        // The && operator checks if the two arrays have any elements in common
        GenerateColumn(property);
        _sql.Append(" && ");
        TranslateConstant(values, false);
    }


    protected override void TranslateQueryParameter(object? value)
    {
        // For null values, simply inline rather than parameterize; parameterized NULLs require setting NpgsqlDbType which is a bit more complicated,
        // plus in any case equality with NULL requires different SQL (x IS NULL rather than x = y)
        if (value is null)
        {
            _sql.Append("NULL");
        }
        else
        {
            ParameterValues.Add(value);
            // The param name is just the index, so there is no need for escaping or quoting.
            _sql.Append('$').Append(_parameterIndex++);
        }
    }
}
