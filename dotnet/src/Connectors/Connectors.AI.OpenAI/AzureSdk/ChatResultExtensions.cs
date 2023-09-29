// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;

using SemanticKernel.AI.ChatCompletion;


/// <summary>
/// Provides extension methods for the IChatResult interface.
/// </summary>
public static class ChatResultExtensions
{
    /// <summary>
    /// Retrieve the resulting function from the chat result.
    /// </summary>
    /// <param name="chatResult"></param>
    /// <returns>The <see cref="OpenAIFunctionResponse"/>, or null if no function was returned by the model.</returns>
    public static OpenAIFunctionResponse? GetFunctionResponse(this IChatResult chatResult)
    {
        OpenAIFunctionResponse? functionResponse = null;
        var functionCall = chatResult.ModelResult.GetResult<ChatModelResult>().Choice.Message.FunctionCall;

        if (functionCall is not null)
        {
            functionResponse = OpenAIFunctionResponse.FromFunctionCall(functionCall);
        }
        return functionResponse;
    }
}
