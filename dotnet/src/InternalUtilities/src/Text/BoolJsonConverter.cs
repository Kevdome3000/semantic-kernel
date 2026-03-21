// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Text;

#pragma warning disable CA1812 // Instantiated via JsonConverterAttribute


/// <summary>
/// Deserializes a bool from a string. This is useful when deserializing a <see cref="PromptExecutionSettings"/> instance that contains bool properties.
/// Serializing a <see cref="PromptExecutionSettings"/> instance without this converter will throw a 'System.Text.Json.JsonException : The JSON value could not be converted to System.Nullable'
/// if there are any bool properties.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class BoolJsonConverter : JsonConverter<bool>
{
    /// <inheritdoc/>
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();

            if (value is null)
            {
                return false;
            }

            if (bool.TryParse(value, out var boolValue))
            {
                return boolValue;
            }

            throw new ArgumentException($"Value '{value}' can be parsed as a boolean value");
        }

        if (reader.TokenType == JsonTokenType.True)
        {
            return true;
        }

        if (reader.TokenType == JsonTokenType.False)
        {
            return false;
        }

        throw new ArgumentException($"Invalid token type found '{reader.TokenType}', expected a boolean value.");
    }


    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
