﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Pinecone;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;


/// <summary>
/// The vector similarity metric of the index
/// </summary>
/// <value>The vector similarity metric of the index</value>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IndexMetric
{
    /// <summary>
    /// Default value.
    /// </summary>
    None = 0,

    /// <summary>
    /// Enum Euclidean for value: euclidean
    /// </summary>
    [EnumMember(Value = "euclidean")]
    Euclidean = 1,

    /// <summary>
    /// Enum Cosine for value: cosine
    /// </summary>
    [EnumMember(Value = "cosine")]
    Cosine = 2,

    /// <summary>
    /// Enum Dotproduct for value: dotproduct
    /// </summary>
    [EnumMember(Value = "dotproduct")]
    Dotproduct = 3
}
