// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
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
            // TODO: This aligns with our mapping of DateTime to PG's timestamp (as opposed to timestamptz) - we probably want to
            // change that to timestamptz (aligning with Npgsql and EF). See #10641.
            case DateTime dateTime:
                _sql.Append('\'').Append(dateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFF", CultureInfo.InvariantCulture)).Append('\'');
                return;
            case DateTimeOffset dateTimeOffset:
                if (dateTimeOffset.Offset != TimeSpan.Zero)
                {
                    throw new NotSupportedException("DateTimeOffset with non-zero offset is not supported with PostgreSQL");
                }

                _sql.Append('\'').Append(dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFF", CultureInfo.InvariantCulture)).Append("Z'");
                return;

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

                    this.TranslateConstant(element, isSearchCondition: false);
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
