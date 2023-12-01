// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Experimental.Assistants.Internal;

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensions;
using Models;


/// <summary>
/// Represents an assistant that can call the model and use tools.
/// </summary>
internal sealed class Assistant : IAssistant
{
    /// <inheritdoc/>
    public string Id => _model.Id;

    /// <inheritdoc/>
    public Kernel Kernel { get; }

    /// <inheritdoc/>
    public KernelPluginCollection Plugins => Kernel.Plugins;

    /// <inheritdoc/>
#pragma warning disable CA1720 // Identifier contains type name - We don't control the schema
#pragma warning disable CA1716 // Identifiers should not match keywords
    public string Object => _model.Object;
#pragma warning restore CA1720 // Identifier contains type name - We don't control the schema
#pragma warning restore CA1716 // Identifiers should not match keywords

    /// <inheritdoc/>
    public long CreatedAt => _model.CreatedAt;

    /// <inheritdoc/>
    public string? Name => _model.Name;

    /// <inheritdoc/>
    public string? Description => _model.Description;

    /// <inheritdoc/>
    public string Model => _model.Model;

    /// <inheritdoc/>
    public string Instructions => _model.Instructions;

    private readonly OpenAIRestContext _restContext;
    private readonly AssistantModel _model;


    /// <summary>
    /// Create a new assistant.
    /// </summary>
    /// <param name="restContext">A context for accessing OpenAI REST endpoint</param>
    /// <param name="assistantModel">The assistant definition</param>
    /// <param name="plugins">Plugins to initialize as assistant tools</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An initialized <see cref="Assistant"> instance.</see></returns>
    public static async Task<IAssistant> CreateAsync(
        OpenAIRestContext restContext,
        AssistantModel assistantModel,
        IEnumerable<IKernelPlugin>? plugins = null,
        CancellationToken cancellationToken = default)
    {
        AssistantModel resultModel =
            await restContext.CreateAssistantModelAsync(assistantModel, cancellationToken).ConfigureAwait(false) ??
            throw new KernelException("Unexpected failure creating assistant: no result.");

        return new Assistant(resultModel, restContext, plugins);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="Assistant"/> class.
    /// </summary>
    internal Assistant(
        AssistantModel model,
        OpenAIRestContext restContext,
        IEnumerable<IKernelPlugin>? plugins = null)
    {
        _model = model;
        _restContext = restContext;

        KernelBuilder builder =
            new KernelBuilder()
                .WithOpenAIChatCompletion(_model.Model, _restContext.ApiKey);

        Kernel = builder.Build();

        if (plugins is not null)
        {
            Kernel.Plugins.AddRange(plugins);
        }
    }


    /// <inheritdoc/>
    public Task<IChatThread> NewThreadAsync(CancellationToken cancellationToken = default) => ChatThread.CreateAsync(_restContext, cancellationToken);


    /// <inheritdoc/>
    public Task<IChatThread> GetThreadAsync(string id, CancellationToken cancellationToken = default) => ChatThread.GetAsync(_restContext, id, cancellationToken);


    /// <summary>
    /// Marshal thread run through <see cref="KernelFunction"/> interface.
    /// </summary>
    /// <param name="input">The user input</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An assistant response (<see cref="AssistantResponse"/></returns>
    [KernelFunction] [Description("Provide input to assistant a response")]
    public async Task<AssistantResponse> AskAsync(
        [Description("The input for the assistant.")]
        string input,
        CancellationToken cancellationToken = default)
    {
        IChatThread? thread = await NewThreadAsync(cancellationToken).ConfigureAwait(false);
        await thread.AddUserMessageAsync(input, cancellationToken).ConfigureAwait(false);
        IEnumerable<IChatMessage>? message = await thread.InvokeAsync(this, cancellationToken).ConfigureAwait(false);
        AssistantResponse response =
            new AssistantResponse
            {
                ThreadId = thread.Id,
                Response = string.Concat(message.Select(m => m.Content))
            };

        return response;
    }
}
