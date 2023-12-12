// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Experimental.Assistants;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Internal;


/// <summary>
/// Specialization of <see cref="KernelPlugin"/> for <see cref="IAssistant"/>
/// </summary>
public abstract class AssistantPlugin : KernelPlugin
{
    /// <inheritdoc/>
    protected AssistantPlugin(string name, string? description = null)
        : base(name, description)
    {
        // No specialization...
    }


    internal abstract Assistant Assistant { get; }


    /// <summary>
    /// Invoke plugin with user input
    /// </summary>
    /// <param name="input">The user input</param>
    /// <param name="cancellationToken">A cancel token</param>
    /// <returns>The assistant response</returns>
    public async Task<string> InvokeAsync(string input, CancellationToken cancellationToken = default)
    {
        var args = new KernelArguments { { "input", input } };
        var result = await this.First().InvokeAsync(this.Assistant.Kernel, args, cancellationToken).ConfigureAwait(false);
        var response = result.GetValue<AssistantResponse>()!;

        return response.Message;
    }
}
