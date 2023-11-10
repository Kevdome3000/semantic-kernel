namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;

using System.Text.Json.Serialization;


public sealed class ResponseFormat
{
    public ResponseFormat() => Type = ChatResponseFormat.Text;

    public ResponseFormat(ChatResponseFormat format) => Type = format;

    [JsonInclude]
    [JsonPropertyName("type")]

    public ChatResponseFormat Type { get; private set; }

    public static implicit operator ChatResponseFormat(ResponseFormat format) => format.Type;

    public static implicit operator ResponseFormat(ChatResponseFormat format) => new(format);

    public static ResponseFormat Text => new(ChatResponseFormat.Text);

    public static ResponseFormat Json => new(ChatResponseFormat.Json);
}
