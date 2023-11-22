﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Planning.Structured;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Memory;
using SemanticFunctions;


/// <summary>
/// Common configuration for planner instances.
/// </summary>
public sealed class StructuredPlannerConfig
{
    /// <summary>
    /// The minimum relevancy score for a function to be considered
    /// </summary>
    /// <remarks>
    /// Depending on the embeddings engine used, the user ask, the step goal
    /// and the functions available, this value may need to be adjusted.
    /// For default, this is set to null to exhibit previous behavior.
    /// </remarks>
    public double? RelevancyThreshold { get; set; }

    /// <summary>
    /// The maximum number of relevant functions to include in the plan.
    /// </summary>
    /// <remarks>
    /// Limits the number of relevant functions as result of semantic
    /// search included in the plan creation request.
    /// <see cref="IncludedFunctions"/> will be included
    /// in the plan regardless of this limit.
    /// </remarks>
    public int MaxRelevantFunctions { get; set; } = 100;

    /// <summary>
    /// A list of skills to exclude from the plan creation request.
    /// </summary>
    public HashSet<string> ExcludedSkills { get; } = new();

    /// <summary>
    /// A list of functions to exclude from the plan creation request.
    /// </summary>
    public HashSet<string> ExcludedFunctions { get; } = new();

    /// <summary>
    /// A list of functions to include in the plan creation request.
    /// </summary>
    public HashSet<string> IncludedFunctions { get; } = new();

    /// <summary>
    /// The maximum number of tokens to allow in a plan.
    /// </summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>
    /// The maximum number of iterations to allow in a plan.
    /// </summary>
    public int MaxIterations { get; set; } = 100;

    /// <summary>
    /// The minimum time to wait between iterations in milliseconds.
    /// </summary>
    public int MinIterationTimeMs { get; set; } = 0;

    /// <summary>
    /// Whether to allow missing functions in the plan on creation.
    /// If set to true, the plan will be created with missing functions as no-op steps.
    /// If set to false (default), the plan creation will fail if any functions are missing.
    /// </summary>
    public bool AllowMissingFunctions { get; set; } = false;

    /// <summary>
    /// Semantic memory to use for function lookup (optional).
    /// </summary>
    public ISemanticTextMemory Memory { get; set; } = NullMemory.Instance;

    /// <summary>
    /// Optional callback to get the available functions for planning.
    /// </summary>
    public Func<StructuredPlannerConfig, string?, CancellationToken, Task<IEnumerable<FunctionView>>>? GetAvailableFunctionsAsync { get; set; }

    /// <summary>
    /// Optional callback to get a function by name.
    /// </summary>
    public Func<string, string, KernelFunction?>? GetSkillFunction { get; set; }

    /// <summary>
    /// Delegate to get the prompt template string.
    /// </summary>
    public Func<string>? GetPromptTemplate { get; set; } = null;

    /// <summary>
    /// The configuration to use for the prompt template.
    /// </summary>
    public PromptTemplateConfig? PromptUserConfig { get; set; } = null;

    /// <summary>
    ///  Serializer options for the planner to use for deserialization
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
}
