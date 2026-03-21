// Copyright (c) Microsoft. All rights reserved.

using MAAI = Microsoft.Agents.AI;

namespace Microsoft.SemanticKernel.Agents;

[Experimental("SKEXP0110")]
internal sealed class SemanticKernelAIAgentThread : MAAI.AgentThread
{
    private readonly Func<AgentThread, JsonSerializerOptions?, JsonElement> _threadSerializer;


    internal SemanticKernelAIAgentThread(AgentThread thread, Func<AgentThread, JsonSerializerOptions?, JsonElement> threadSerializer)
    {
        Throw.IfNull(thread);
        Throw.IfNull(threadSerializer);

        InnerThread = thread;
        _threadSerializer = threadSerializer;
    }


    /// <summary>
    /// Gets the underlying Semantic Kernel Agent Framework <see cref="AgentThread"/>.
    /// </summary>
    public AgentThread InnerThread { get; }


    /// <inheritdoc />
    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return this._threadSerializer(InnerThread, jsonSerializerOptions);
    }


    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        Throw.IfNull(serviceType);

        return serviceKey is null && serviceType.IsInstanceOfType(InnerThread)
            ? InnerThread
            : base.GetService(serviceType, serviceKey);
    }
}
