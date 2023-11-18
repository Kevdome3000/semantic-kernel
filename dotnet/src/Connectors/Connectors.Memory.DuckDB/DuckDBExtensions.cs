// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Memory.DuckDB;

using System.Data.Common;


internal static class DuckDBExtensions
{
    public static T GetFieldValue<T>(this DbDataReader reader, string fieldName)
    {
        int ordinal = reader.GetOrdinal(fieldName);
        return reader.GetFieldValue<T>(ordinal);
    }
}
