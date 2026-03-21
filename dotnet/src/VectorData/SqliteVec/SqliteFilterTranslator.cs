// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.SqliteVec;

internal sealed class SqliteFilterTranslator : SqlFilterTranslator
{
    private readonly Dictionary<string, object> _parameters = [];


    internal SqliteFilterTranslator(CollectionModel model, LambdaExpression lambdaExpression)
        : base(model, lambdaExpression, sql: null)
    {
    }


    internal Dictionary<string, object> Parameters => _parameters;


    protected override void TranslateConstant(object? value, bool isSearchCondition)
    {
        switch (value)
        {
            case Guid g:
                // Microsoft.Data.Sqlite writes GUIDs as upper-case strings, align our constant formatting with that.
                _sql.Append('\'').Append(g.ToString().ToUpperInvariant()).Append('\'');
                break;
            case DateTime dateTime:
                _sql.Append('\'').Append(dateTime.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF", System.Globalization.CultureInfo.InvariantCulture)).Append('\'');
                break;
            case DateTimeOffset dateTimeOffset:
                _sql.Append('\'').Append(dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFzzz", System.Globalization.CultureInfo.InvariantCulture)).Append('\'');
                break;
#if NET
            case DateOnly dateOnly:
                this._sql.Append('\'').Append(dateOnly.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)).Append('\'');
                break;
            case TimeOnly timeOnly:
                this._sql.Append('\'').Append(timeOnly.ToString("HH:mm:ss.FFFFFFF", System.Globalization.CultureInfo.InvariantCulture)).Append('\'');
                break;
#endif
            default:
                base.TranslateConstant(value, isSearchCondition);
                break;
        }
    }


    // TODO: support Contains over array fields (#10343)
    protected override void TranslateContainsOverArrayColumn(Expression source, Expression item)
    {
        throw new NotSupportedException("Unsupported Contains expression");
    }


    // TODO: support Any over array fields (#10343)
    protected override void TranslateAnyContainsOverArrayColumn(PropertyModel property, object? values)
    {
        throw new NotSupportedException("Unsupported method call: Enumerable.Any");
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
            // The param name is just the index, so there is no need for escaping or quoting.
            int index = _sql.Length;
            _sql.Append('@').Append(_parameters.Count + 1);
            string paramName = _sql.ToString(index, _sql.Length - index);
            _parameters.Add(paramName, value);
        }
    }
}
