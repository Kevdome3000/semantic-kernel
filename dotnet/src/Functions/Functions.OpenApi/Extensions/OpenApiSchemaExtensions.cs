// Copyright (c) Microsoft. All rights reserved.

using System.Globalization;
using System.Text;
using Microsoft.OpenApi;

namespace Microsoft.SemanticKernel.Plugins.OpenApi;

internal static class OpenApiSchemaExtensions
{
    /// <summary>
    /// Gets a JSON serialized representation of an <see cref="IOpenApiSchema"/>
    /// </summary>
    /// <param name="schema">The schema.</param>
    /// <returns>An instance of <see cref="KernelJsonSchema"/> that contains the JSON Schema.</returns>
    internal static KernelJsonSchema ToJsonSchema(this IOpenApiSchema schema)
    {
        var schemaBuilder = new StringBuilder();
        var jsonWriter = new OpenApiJsonWriter(new StringWriter(schemaBuilder, CultureInfo.InvariantCulture));
        jsonWriter.Settings.InlineLocalReferences = true;
        schema.SerializeAsV31(jsonWriter);
        return KernelJsonSchema.Parse(schemaBuilder.ToString());
    }

    /// <summary>
    /// Gets the schema type as a string from the <see cref="JsonSchemaType"/> flags enum.
    /// </summary>
    /// <param name="schema">The schema.</param>
    /// <returns>The type as a lowercase string, or empty string if null.</returns>
    internal static string GetSchemaType(this IOpenApiSchema schema)
    {
        var type = schema.Type;
        if (type is null)
        {
            return string.Empty;
        }

        // JsonSchemaType is a flags enum; for single values, get the lowercase name.
        // If multiple types are combined (e.g. String | Null), return the non-null type.
        var effectiveType = type.Value & ~JsonSchemaType.Null;
        if (effectiveType == 0)
        {
            return "null";
        }

        return effectiveType.ToString().ToLowerInvariant();
    }
}
