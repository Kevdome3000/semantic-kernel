// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.SqliteVec;

internal sealed class SqliteWhereInCondition(string operand, List<object> values)
    : SqliteWhereCondition(operand, values)
{
    public override string BuildQuery(List<string> parameterNames)
    {
        const string InOperator = "IN";

        Verify.True(parameterNames.Count > 0, $"Cannot build '{nameof(SqliteWhereInCondition)}' condition without parameter names.");

        return $"{GetOperand()} {InOperator} ({string.Join(", ", parameterNames)})";
    }
}
