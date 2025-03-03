﻿// Copyright (c) Microsoft.All rights reserved.

namespace Microsoft.Extensions.VectorData;

/// <summary>
/// Defines options to use with the <see cref="IVectorStoreRecordMapper{TRecordDataModel, TStorageModel}.MapFromStorageToDataModel"/> method.
/// </summary>
public class StorageToDataModelMapperOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to include vectors in the retrieval result.
    /// </summary>
    public bool IncludeVectors { get; init; } = false;
}
