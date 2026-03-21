// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.CosmosNoSql;

internal sealed class ClientWrapper : IDisposable
{
    private readonly bool _ownsClient;
    private int _referenceCount = 1;


    internal ClientWrapper(CosmosClient cosmosClient, bool ownsClient)
    {
        Client = cosmosClient;
        _ownsClient = ownsClient;
    }


    internal CosmosClient Client { get; }


    internal ClientWrapper Share()
    {
        if (_ownsClient)
        {
            Interlocked.Increment(ref _referenceCount);
        }

        return this;
    }


    public void Dispose()
    {
        if (_ownsClient)
        {
            if (Interlocked.Decrement(ref _referenceCount) == 0)
            {
                Client.Dispose();
            }
        }
    }
}
