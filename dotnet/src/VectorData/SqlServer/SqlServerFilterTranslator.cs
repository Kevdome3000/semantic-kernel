// Copyright (c) Microsoft. All rights reserved.

#if NET
using System.Globalization;
#endif

namespace Microsoft.SemanticKernel.Connectors.SqlServer;

internal sealed class SqlServerFilterTranslator : SqlFilterTranslator
{
    private readonly List<object> _parameterValues = [];
    private readonly string? _tableAlias;
    private int _parameterIndex;


    internal SqlServerFilterTranslator(
        CollectionModel model,
        LambdaExpression lambdaExpression,
        StringBuilder sql,
        int startParamIndex,
        string? tableAlias = null)
        : base(model, lambdaExpression, sql)
    {
        _parameterIndex = startParamIndex;
        _tableAlias = tableAlias;
    }


    internal List<object> ParameterValues => _parameterValues;


    protected override void TranslateConstant(object? value, bool isSearchCondition)
    {
        switch (value)
        {
            case bool boolValue when isSearchCondition:
                _sql.Append(boolValue
                    ? "1 = 1"
                    : "1 = 0");
                return;
            case bool boolValue:
                _sql.Append(boolValue
                    ? "CAST(1 AS BIT)"
                    : "CAST(0 AS BIT)");
                return;
            case DateTime dateTime:
                _sql.Append('\'').Append(dateTime.ToString("o")).Append('\'');
                return;
            case DateTimeOffset dateTimeOffset:
                _sql.Append('\'').Append(dateTimeOffset.ToString("o")).Append('\'');
                return;
#if NET
            case DateOnly dateOnly:
                this._sql.Append('\'').Append(dateOnly.ToString("o")).Append('\'');
                return;
            case TimeOnly timeOnly:
                this._sql.AppendFormat(timeOnly.Ticks % 10000000 == 0
                    ? string.Format(CultureInfo.InvariantCulture, @"'{0:HH\:mm\:ss}'", value)
                    : string.Format(CultureInfo.InvariantCulture, @"'{0:HH\:mm\:ss\.FFFFFFF}'", value));
                return;
#endif

            default:
                base.TranslateConstant(value, isSearchCondition);
                break;
        }
    }


    protected override void GenerateColumn(PropertyModel property, bool isSearchCondition = false)
    {
        // StorageName is considered to be a safe input, we quote and escape it mostly to produce valid SQL.
        if (_tableAlias is not null)
        {
            _sql.Append(_tableAlias).Append('.');
        }
        _sql.Append('[').Append(property.StorageName.Replace("]", "]]")).Append(']');

        // "SELECT * FROM MyTable WHERE BooleanColumn;" is not supported.
        // "SELECT * FROM MyTable WHERE BooleanColumn = 1;" is supported.
        if (isSearchCondition)
        {
            _sql.Append(" = 1");
        }
    }


    protected override void TranslateContainsOverArrayColumn(Expression source, Expression item)
    {
        if (item.Type != typeof(string))
        {
            throw new NotSupportedException("Unsupported Contains expression");
        }

        _sql.Append("JSON_CONTAINS(");
        Translate(source);
        _sql.Append(", ");
        Translate(item);
        _sql.Append(") = 1");
    }


    protected override void TranslateContainsOverParameterizedArray(Expression source, Expression item, object? value)
    {
        if (value is not IEnumerable elements)
        {
            throw new NotSupportedException("Unsupported Contains expression");
        }

        Translate(item);
        _sql.Append(" IN (");

        var isFirst = true;

        foreach (var element in elements)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                _sql.Append(", ");
            }

            TranslateConstant(element, false);
        }

        _sql.Append(')');
    }


    protected override void TranslateAnyContainsOverArrayColumn(PropertyModel property, object? values)
    {
        // Translate r.Strings.Any(s => array.Contains(s)) to:
        // EXISTS(SELECT 1 FROM OPENJSON(column) WHERE value IN ('a', 'b', 'c'))
        if (values is not IEnumerable elements)
        {
            throw new NotSupportedException("Unsupported Any expression");
        }

        _sql.Append("EXISTS(SELECT 1 FROM OPENJSON(");
        GenerateColumn(property);
        _sql.Append(") WHERE value IN (");

        var isFirst = true;

        foreach (var element in elements)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                _sql.Append(", ");
            }

            TranslateConstant(element, false);
        }

        _sql.Append("))");
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
            _parameterValues.Add(value);
            // The param name is just the index, so there is no need for escaping or quoting.
            // SQL Server parameters can't start with a digit (but underscore is OK).
            _sql.Append("@_").Append(_parameterIndex++);
        }
    }
}
