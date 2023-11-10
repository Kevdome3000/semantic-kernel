namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChatResponseFormat
{
    [EnumMember(Value = "text")]
    Text,

    [EnumMember(Value = "json_object")]
    Json
}
