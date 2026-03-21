// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateOperationResult
{
    private const string Success = nameof(Success);

    [JsonPropertyName("errors")]
    public WeaviateOperationResultErrors? Errors { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonIgnore]
    public bool? IsSuccess => Status?.Equals(Success, StringComparison.OrdinalIgnoreCase);
}
