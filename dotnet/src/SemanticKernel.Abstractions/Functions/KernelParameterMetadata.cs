﻿// Copyright (c) Microsoft.All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel;
/// <summary>
/// Provides read-only metadata for a <see cref="KernelFunction"/> parameter.
/// </summary>
public sealed class KernelParameterMetadata
{
    /// <summary>The name of the parameter.</summary>
    private string _name = string.Empty;

    /// <summary>The description of the parameter.</summary>
    private string _description = string.Empty;

    /// <summary>The default value of the parameter.</summary>
    private object? _defaultValue;

    /// <summary>The .NET type of the parameter.</summary>
    private Type? _parameterType;

    /// <summary>The schema of the parameter, potentially lazily-initialized.</summary>
    private InitializedSchema? _schema;
    /// <summary>The serializer options to generate JSON schema.</summary>
    private readonly JsonSerializerOptions? _jsonSerializerOptions;

    /// <summary>Initializes the <see cref="KernelParameterMetadata"/> for a parameter with the specified name.</summary>
    /// <param name="name">The name of the parameter.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="name"/> was null.</exception>
    /// <exception cref="ArgumentException">The <paramref name="name"/> was empty or composed entirely of whitespace.</exception>
    [RequiresUnreferencedCode("Uses reflection to generate schema, making it incompatible with AOT scenarios.")]
    [RequiresDynamicCode("Uses reflection to generate schema, making it incompatible with AOT scenarios.")]
    public KernelParameterMetadata(string name)
        : this(name, null!)
    { }

    /// <summary>Initializes the <see cref="KernelParameterMetadata"/> for a parameter with the specified name.</summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to generate JSON schema.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="name"/> was null.</exception>
    /// <exception cref="ArgumentException">The <paramref name="name"/> was empty or composed entirely of whitespace.</exception>
    public KernelParameterMetadata(string name, JsonSerializerOptions jsonSerializerOptions)
    {
        Name = name;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <summary>Initializes a <see cref="KernelParameterMetadata"/> as a copy of another <see cref="KernelParameterMetadata"/>.</summary>
    /// <exception cref="ArgumentNullException">The <paramref name="metadata"/> was null.</exception>
    /// <remarks>This creates a shallow clone of <paramref name="metadata"/>.</remarks>
    [RequiresUnreferencedCode("Uses reflection, if no JSOs are available in the metadata, to generate the schema, making it incompatible with AOT scenarios.")]
    [RequiresDynamicCode("Uses reflection, if no JSOs are available in the metadata, to generate the schema, making it incompatible with AOT scenarios.")]
    public KernelParameterMetadata(KernelParameterMetadata metadata)
    {
        Verify.NotNull(metadata);
        _name = metadata._name;
        _description = metadata._description;
        _defaultValue = metadata._defaultValue;
        IsRequired = metadata.IsRequired;
        _parameterType = metadata._parameterType;
        _schema = metadata._schema;
        _jsonSerializerOptions = metadata._jsonSerializerOptions;
    }

    /// <summary>Initializes a <see cref="KernelParameterMetadata"/> as a copy of another <see cref="KernelParameterMetadata"/>.</summary>
    /// <exception cref="ArgumentNullException">The <paramref name="metadata"/> was null.</exception>
    /// <param name="metadata">The metadata to copy.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to generate JSON schema.</param>
    /// <remarks>This creates a shallow clone of <paramref name="metadata"/>.</remarks>
    public KernelParameterMetadata(KernelParameterMetadata metadata, JsonSerializerOptions jsonSerializerOptions)
    {
        Verify.NotNull(metadata);
        _name = metadata._name;
        _description = metadata._description;
        _defaultValue = metadata._defaultValue;
        IsRequired = metadata.IsRequired;
        _parameterType = metadata._parameterType;
        _schema = metadata._schema;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <summary>Gets the name of the function.</summary>
    public string Name
    {
        get => _name;
        set
        {
            Verify.NotNullOrWhiteSpace(value);
            _name = value;
        }
    }

    /// <summary>Gets a description of the function, suitable for use in describing the purpose to a model.</summary>
    [AllowNull]
    public string Description
    {
        get => _description;
        set
        {
            string newDescription = value ?? string.Empty;

            if (value != _description && _schema?.Inferred is true)
            {
                _schema = null;
            }

            _description = newDescription;
        }
    }

    /// <summary>Gets the default value of the parameter.</summary>
    public object? DefaultValue
    {
        get => _defaultValue;
        set
        {
            if (value != _defaultValue && _schema?.Inferred is true)
            {
                _schema = null;
            }

            _defaultValue = value;
        }
    }

    /// <summary>Gets whether the parameter is required.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets the .NET type of the parameter.</summary>
    [JsonIgnore]
    public Type? ParameterType
    {
        get => _parameterType;
        set
        {
            if (value != _parameterType && _schema?.Inferred is true)
            {
                _schema = null;
            }

            _parameterType = value;
        }
    }

    /// <summary>Gets a JSON Schema describing the parameter's type.</summary>
    public KernelJsonSchema? Schema
    {
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "The warning is shown and should be addressed at the class creation site; no need to show it again at the members invocation sites.")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "The warning is shown and should be addressed at the class creation site; no need to show it again at the members invocation sites.")]
        get => (_schema ??= InferSchema(ParameterType,
            DefaultValue,
            Description,
            _jsonSerializerOptions)).Schema;
        set => _schema = value is null
            ? null
            : new() { Inferred = false, Schema = value };
    }

    /// <summary>Infers a JSON schema from a <see cref="Type"/> and description.</summary>
    /// <param name="parameterType">The parameter type. If null, no schema can be inferred.</param>
    /// <param name="defaultValue">The parameter's default value, if any.</param>
    /// <param name="description">The parameter description. If null, it won't be included in the schema.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to generate JSON schema.</param>
    [RequiresUnreferencedCode("Uses reflection if no JSOs are provided, making it incompatible with AOT scenarios.")]
    [RequiresDynamicCode("Uses reflection if no JSOs are provided, making it incompatible with AOT scenarios.")]
    internal static InitializedSchema InferSchema(Type? parameterType, object? defaultValue, string? description, JsonSerializerOptions? jsonSerializerOptions)
    {
        KernelJsonSchema? schema = null;

        // If no schema was provided but a type was provided, try to generate a schema from the type.
        if (parameterType is not null)
        {
            // Type must be usable as a generic argument to be used with JsonSchemaBuilder.
            bool invalidAsGeneric =
                // from RuntimeType.ThrowIfTypeNeverValidGenericArgument
#if NET_8_OR_GREATER
                parameterType.IsFunctionPointer ||
#endif
                parameterType.IsPointer ||
                parameterType.IsByRef ||
                parameterType == typeof(void);

            if (!invalidAsGeneric)
            {
                try
                {
                    if (InternalTypeConverter.ConvertToString(defaultValue) is string stringDefault && !string.IsNullOrWhiteSpace(stringDefault))
                    {
                        bool needsSpace = !string.IsNullOrWhiteSpace(description);
                        description += $"{(needsSpace ? " " : "")}(default value: {stringDefault})";
                    }

                    schema = jsonSerializerOptions is not null
                        ? KernelJsonSchemaBuilder.Build(parameterType, jsonSerializerOptions, description)
                        : KernelJsonSchemaBuilder.Build(parameterType, description);
                }
                catch (ArgumentException)
                {
                    // Invalid type; ignore, and leave schema as null.
                    // This should be exceedingly rare, as we checked for all known category of
                    // problematic types above. If it becomes more common that schema creation
                    // could fail expensively, we'll want to track whether inference was already
                    // attempted and avoid doing so on subsequent accesses if it was.
                }
            }
        }

        // Always return an instance so that subsequent reads of the Schema don't try to regenerate
        // it again. If inference failed, we just leave the Schema null in the instance.
        return new InitializedSchema { Inferred = true, Schema = schema };
    }

    /// <summary>A wrapper for a <see cref="KernelJsonSchema"/> and whether it was inferred or set explicitly by the user.</summary>
    internal sealed class InitializedSchema
    {
        /// <summary>true if the <see cref="Schema"/> was inferred; false if it was set explicitly by the user.</summary>
        public bool Inferred { get; set; }

        /// <summary>The schema, if one exists.</summary>
        public KernelJsonSchema? Schema { get; set; }
    }
}
