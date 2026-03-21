// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.SqliteVec;

internal abstract class SqliteWhereCondition(string operand, List<object> values)
{
    public string Operand { get; set; } = operand;

    public List<object> Values { get; set; } = values;

    public string? TableName { get; set; }

    public abstract string BuildQuery(List<string> parameterNames);


    protected string GetOperand()
    {
        return !string.IsNullOrWhiteSpace(TableName)
            ? $"\"{TableName}\".\"{Operand}\""
            : $"\"{Operand}\"";
    }
}
