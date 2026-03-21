// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.SqliteVec;

internal sealed class SqliteWhereMatchCondition(string operand, object value)
    : SqliteWhereCondition(operand, [value])
{
    public override string BuildQuery(List<string> parameterNames)
    {
        const string MatchOperator = "MATCH";

        Verify.True(parameterNames.Count > 0, $"Cannot build '{nameof(SqliteWhereMatchCondition)}' condition without parameter name.");

        return $"{GetOperand()} {MatchOperator} {parameterNames[0]}";
    }
}
