// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130
namespace Microsoft.SemanticKernel;

using System;
using System.Collections.Generic;
using AI;

#pragma warning restore IDE0130


/// <summary>
/// Represents arguments for various SK component methods, such as KernelFunction.InvokeAsync, Kernel.InvokeAsync, IPromptTemplate.RenderAsync, and more.
/// </summary>
public sealed class KernelArguments : Dictionary<string, string>
{
    /// <summary>
    /// The main input parameter name.
    /// </summary>
    public const string InputParameterName = "input";


    public KernelArguments(string input, PromptExecutionSettings? executionSettings = null) : this(executionSettings)
    {
        Set(InputParameterName, input);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="KernelArguments"/> class with the specified AI request settings.
    /// </summary>
    /// <param name="executionSettings">The prompt execution settings.</param>
    public KernelArguments(PromptExecutionSettings? executionSettings = null) : base(StringComparer.OrdinalIgnoreCase) => ExecutionSettings = executionSettings;


    /// <summary>Gets the main input string.</summary>
    /// <remarks>If the main input string was removed from the collection, an empty string will be returned.</remarks>
    public string Input => TryGetValue(InputParameterName, out string? value) ? value : string.Empty;


    /// <summary>
    /// Updates the main input text with the new value after a function is complete.
    /// </summary>
    /// <param name="value">The new input value, for the next function in the pipeline, or as a result for the user
    /// if the pipeline reached the end.</param>
    /// <returns>The current instance</returns>
    public KernelArguments Update(string? value)
    {
        Set(InputParameterName, value);
        return this;
    }


    /// <summary>
    /// This method allows to store additional data in the context variables, e.g. variables needed by functions in the
    /// pipeline. These "variables" are visible also to semantic functions using the "{{varName}}" syntax, allowing
    /// to inject more information into prompt templates.
    /// </summary>
    /// <param name="name">Variable name</param>
    /// <param name="value">Value to store. If the value is NULL the variable is deleted.</param>
    public void Set(string name, string? value)
    {
        Verify.NotNullOrWhiteSpace(name);

        if (value != null)
        {
            this[name] = value;
        }
        else
        {
            Remove(name);
        }
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="KernelArguments"/> class that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="source">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied the new <see cref="KernelArguments"/>.</param>
    public KernelArguments(IDictionary<string, string> source) : this()
    {
        foreach (KeyValuePair<string, string> x in source) { this[x.Key] = x.Value; }
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="KernelArguments"/> class that contains AI request settings and elements copied from the specified <see cref="KernelArguments"/>
    /// </summary>
    /// <param name="other">The <see cref="KernelArguments"/> whose AI request setting and elements are copied to the new <see cref="KernelArguments"/>.</param>
    public KernelArguments(KernelArguments other) : this(other as IDictionary<string, string>) => ExecutionSettings = other.ExecutionSettings;


    /// <summary>
    /// Print the processed input, aka the current data after any processing occurred.
    /// </summary>
    /// <returns>Processed input, aka result</returns>
    public override string ToString() => Input;


    /// <summary>
    /// Gets or sets the prompt execution settings
    /// </summary>
    public PromptExecutionSettings? ExecutionSettings { get; set; }
}
