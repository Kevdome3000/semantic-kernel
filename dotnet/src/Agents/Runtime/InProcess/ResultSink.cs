// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Microsoft.SemanticKernel.Agents.Runtime.InProcess;

internal interface IResultSink<TResult> : IValueTaskSource<TResult>
{
    void SetResult(TResult result);
    void SetException(Exception exception);
    void SetCancelled(OperationCanceledException? exception = null);

    ValueTask<TResult> Future { get; }
}


internal sealed class ResultSink<TResult> : IResultSink<TResult>
{
    private ManualResetValueTaskSourceCore<TResult> _core;

    public bool IsCancelled { get; private set; }


    public TResult GetResult(short token)
    {
        return _core.GetResult(token);
    }


    public ValueTaskSourceStatus GetStatus(short token)
    {
        return _core.GetStatus(token);
    }


    public void OnCompleted(
        Action<object?> continuation,
        object? state,
        short token,
        ValueTaskSourceOnCompletedFlags flags)
    {
        _core.OnCompleted(continuation,
            state,
            token,
            flags);
    }


    public void SetCancelled(OperationCanceledException? exception = null)
    {
        IsCancelled = true;
        _core.SetException(exception ?? new OperationCanceledException());
    }


    public void SetException(Exception exception)
    {
        _core.SetException(exception);
    }


    public void SetResult(TResult result)
    {
        _core.SetResult(result);
    }


    public ValueTask<TResult> Future => new(this, _core.Version);
}
