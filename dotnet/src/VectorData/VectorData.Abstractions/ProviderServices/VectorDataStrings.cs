// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.VectorData.ProviderServices;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


/// <summary>
/// Exposes methods for constructing strings that should be used by providers when throwing exceptions.
/// </summary>
[Experimental("MEVD9001")]
public static class VectorDataStrings
{
    public static string ConfiguredEmbeddingTypeIsUnsupportedByTheGenerator(VectorPropertyModel vectorProperty, Type userRequestedEmbeddingType, string supportedVectorTypes)
    {
        return $"Vector property '{vectorProperty.ModelName}' has embedding type '{userRequestedEmbeddingType.TypeName()}' configured, but that type isn't supported by your embedding generator.";
    }


    public static string ConfiguredEmbeddingTypeIsUnsupportedByTheProvider(VectorPropertyModel vectorProperty, Type userRequestedEmbeddingType, string supportedVectorTypes)
    {
        return $"Vector property '{vectorProperty.ModelName}' has embedding type '{userRequestedEmbeddingType.TypeName()}' configured, but that type isn't supported by your provider. Supported types are {supportedVectorTypes}.";
    }


    public static string EmbeddingGeneratorWithInvalidEmbeddingType(VectorPropertyModel vectorProperty)
    {
        return $"An embedding generator was configured on property '{vectorProperty.ModelName}', but output embedding type '{vectorProperty.EmbeddingType.Name}' isn't supported by the connector.";
    }


    public static string EmbeddingPropertyTypeIncompatibleWithEmbeddingGenerator(VectorPropertyModel vectorProperty)
    {
        return $"Property '{vectorProperty.ModelName}' has embedding type '{vectorProperty.Type.TypeName()}', but an embedding generator is configured on the property. Remove the embedding generator or change the property's .NET type to a non-embedding input type to the generator (e.g. string).";
    }


    public static string DifferentEmbeddingTypeSpecifiedForNativelySupportedType(VectorPropertyModel vectorProperty, Type embeddingType)
    {
        return $"Property '{vectorProperty.ModelName}' has {nameof(VectorStoreVectorProperty.EmbeddingType)} configured to '{embeddingType.TypeName()}', but the property already has natively supported '{vectorProperty.Type.TypeName()}'. {nameof(VectorStoreVectorProperty.EmbeddingType)} only needs to be specified for properties that require embedding generation.";
    }


    public static string GetCollectionWithDictionaryNotSupported
        => "Dynamic mapping via Dictionary<string, object?> is not supported via this method, call GetDynamicCollection() instead.";

    public static string IncludeVectorsNotSupportedWithEmbeddingGeneration
        => "When an embedding generator is configured, `Include Vectors` cannot be enabled.";


    public static string IncompatibleEmbeddingGenerator(VectorPropertyModel vectorProperty, IEmbeddingGenerator embeddingGenerator, string supportedOutputTypes)
    {
        return $"Embedding generator '{TypeName(embeddingGenerator.GetType())}' on vector property '{vectorProperty.ModelName}' cannot convert the input type '{vectorProperty.Type.TypeName()}' to a supported vector type (one of: {supportedOutputTypes}).";
    }


    public static string IncompatibleEmbeddingGeneratorWasConfiguredForInputType(Type inputType, Type embeddingGeneratorType)
    {
        return $"An input of type '{inputType.TypeName()}' was provided, but an incompatible embedding generator of type '{embeddingGeneratorType.TypeName()}' was configured.";
    }


    public static string InvalidSearchInputAndNoEmbeddingGeneratorWasConfigured(Type inputType, string supportedVectorTypes)
    {
        return $"A value of type '{inputType.TypeName()}' was passed to 'SearchAsync', but that isn't a supported vector type by your provider and no embedding generator was configured. The supported vector types are: {supportedVectorTypes}.";
    }


    public static string MissingTypeOnPropertyDefinition(VectorStoreProperty property)
    {
        return $"Property '{property.Name}' has no type specified in its definition, and does not have a corresponding .NET property. Specify the type on the definition.";
    }


    public static string UnsupportedVectorPropertyWithoutEmbeddingGenerator(VectorPropertyModel vectorProperty)
    {
        return $"Vector property '{vectorProperty.ModelName}' has type '{vectorProperty.Type.TypeName()}' which isn't supported by your provider, and no embedding generator is configured. Configure a generator that supports converting '{vectorProperty.Type.TypeName()}' to vector type supported by your provider.";
    }


    public static string NonDynamicCollectionWithDictionaryNotSupported(Type dynamicCollectionType)
    {
        return $"Dynamic mapping via Dictionary<string, object?> is not supported via this class, use '{dynamicCollectionType.TypeName()}' instead.";
    }


    private static string TypeName(this Type type)
    {
        var i = type.Name.IndexOf('`');

        if (i == -1)
        {
            return type.Name switch
            {
                "Int32" => "int",
                "Int64" => "long",
                "Boolean" => "bool",
                "Double" => "double",
                "Single" => "float",
                "String" => "string",

                _ => type.Name
            };
        }

        var genericTypeName = type.Name.Substring(0, i);
        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(t => t.TypeName()));
        return $"{genericTypeName}<{genericArgs}>";
    }
}
