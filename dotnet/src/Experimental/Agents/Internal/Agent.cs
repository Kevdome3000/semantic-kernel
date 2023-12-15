// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Experimental.Agents.Internal;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Exceptions;
using Models;


/// <summary>
/// Represents an agent that can call the model and use tools.
/// </summary>
internal sealed class Agent : IAgent
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

    private static readonly Regex s_removeInvalidCharsRegex = new("[^0-9A-Za-z-]");

    private readonly OpenAIRestContext _restContext;
    private readonly AssistantModel _model;

    private AgentPlugin? _agentPlugin;
    private bool _isDeleted;


    /// <summary>
    /// Create a new agent.
    /// </summary>
    /// <param name="restContext">A context for accessing OpenAI REST endpoint</param>
    /// <param name="assistantModel">The assistant definition</param>
    /// <param name="plugins">Plugins to initialize as agent tools</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An initialized <see cref="Agent"> instance.</see></returns>
    public static async Task<IAgent> CreateAsync(
        OpenAIRestContext restContext,
        AssistantModel assistantModel,
        IEnumerable<KernelPlugin>? plugins = null,
        CancellationToken cancellationToken = default)
    {
        AssistantModel? resultModel = await restContext.CreateAssistantModelAsync(assistantModel, cancellationToken).ConfigureAwait(false);

        return new Agent(resultModel, restContext, plugins);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="Agent"/> class.
    /// </summary>
    internal Agent(
        AssistantModel assistantModel,
        OpenAIRestContext restContext,
        IEnumerable<KernelPlugin>? plugins = null)
    {
        _model = assistantModel;
        _restContext = restContext;
        Kernel =
            Kernel
                .CreateBuilder()
                .AddOpenAIChatCompletion(_model.Model, _restContext.ApiKey)
                .Build();

        if (plugins is not null)
        {
            Kernel.Plugins.AddRange(plugins);
        }
    }


    public AgentPlugin AsPlugin() => _agentPlugin ??= DefinePlugin();


    /// <inheritdoc/>
    public Task<IAgentThread> NewThreadAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDeleted();

        return ChatThread.CreateAsync(_restContext, cancellationToken);
    }


    /// <inheritdoc/>
    public Task<IAgentThread> GetThreadAsync(string id, CancellationToken cancellationToken = default)
    {
        ThrowIfDeleted();

        return ChatThread.GetAsync(_restContext, id, cancellationToken);
    }


    /// <inheritdoc/>
    public async Task DeleteThreadAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        await _restContext.DeleteThreadModelAsync(id!, cancellationToken).ConfigureAwait(false);
    }


    /// <inheritdoc/>
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (_isDeleted)
        {
            return;
        }

        await _restContext.DeleteAssistantModelAsync(Id, cancellationToken).ConfigureAwait(false);
        _isDeleted = true;
    }


    /// <summary>
    /// Marshal thread run through <see cref="KernelFunction"/> interface.
    /// </summary>
    /// <param name="input">The user input</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An agent response (<see cref="AgentResponse"/></returns>
    private async Task<AgentResponse> AskAsync(
        [Description("The user message provided to the agent.")]
        string input,
        CancellationToken cancellationToken = default)
    {
        IAgentThread? thread = await NewThreadAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await thread.AddUserMessageAsync(input, cancellationToken).ConfigureAwait(false);

            IChatMessage[]? messages = await thread.InvokeAsync(this, cancellationToken).ToArrayAsync(cancellationToken).ConfigureAwait(false);
            AgentResponse response =
                new AgentResponse
                {
                    ThreadId = thread.Id,
                    Message = string.Concat(messages.Select(m => m.Content))
                };

            return response;
        }
        finally
        {
            await thread.DeleteAsync(cancellationToken).ConfigureAwait(false);
        }
    }


    private AgentPluginImpl DefinePlugin()
    {
        KernelFunction functionAsk = KernelFunctionFactory.CreateFromMethod(AskAsync, description: Description);

        return new AgentPluginImpl(this, functionAsk);
    }


    private void ThrowIfDeleted()
    {
        if (_isDeleted)
        {
            throw new AgentException($"{nameof(Agent)}: {Id} has been deleted.");
        }
    }


    private sealed class AgentPluginImpl : AgentPlugin
    {
        public KernelFunction FunctionAsk { get; }

        internal override Agent Agent { get; }

        public override int FunctionCount => 1;

        private static readonly string s_functionName = nameof(Agent.AskAsync).Substring(0, nameof(AgentPluginImpl.Agent.AskAsync).Length - 5);


        public AgentPluginImpl(Agent agent, KernelFunction functionAsk)
            : base(s_removeInvalidCharsRegex.Replace(agent.Name ?? agent.Id, string.Empty),
                agent.Description ?? agent.Instructions)
        {
            Agent = agent;
            FunctionAsk = functionAsk;
        }


        public override IEnumerator<KernelFunction> GetEnumerator()
        {
            yield return FunctionAsk;
        }


        public override bool TryGetFunction(string name, [NotNullWhen(true)] out KernelFunction? function)
        {
            function = null;

            if (s_functionName.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                function = FunctionAsk;
            }

            return function != null;
        }
    }
}
