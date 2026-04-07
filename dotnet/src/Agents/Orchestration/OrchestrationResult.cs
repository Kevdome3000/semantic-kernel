// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents.Runtime;

namespace Microsoft.SemanticKernel.Agents.Orchestration;

/// <summary>
/// Represents the result of an orchestration operation that yields a value of type <typeparamref name="TValue"/>.
/// This class encapsulates the asynchronous completion of an orchestration process.
/// </summary>
/// <typeparam name="TValue">The type of the value produced by the orchestration.</typeparam>
public sealed class OrchestrationResult<TValue> : IDisposable
{
    private readonly OrchestrationContext _context;
    private readonly CancellationTokenSource _cancelSource;
    private readonly TaskCompletionSource<TValue> _completion;
    private readonly ILogger _logger;
    private bool _isDisposed;

    internal OrchestrationResult(
        OrchestrationContext context,
        TaskCompletionSource<TValue> completion,
        CancellationTokenSource orchestrationCancelSource,
        ILogger logger)
    {
        _cancelSource = orchestrationCancelSource;
        _context = context;
        _completion = completion;
        _logger = logger;
    }

    /// <summary>
    /// Releases all resources used by the <see cref="OrchestrationResult{TValue}"/> instance.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the orchestration name associated with this orchestration result.
    /// </summary>
    public string Orchestration => _context.Orchestration;

    /// <summary>
    /// Gets the topic identifier associated with this orchestration result.
    /// </summary>
    public TopicId Topic => _context.Topic;

    /// <summary>
    /// Asynchronously retrieves the orchestration result value.
    /// If a timeout is specified, the method will throw a <see cref="TimeoutException"/>
    /// if the orchestration does not complete within the allotted time.
    /// </summary>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the maximum wait duration.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TValue}"/> representing the result of the orchestration.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
    /// <exception cref="TimeoutException">Thrown if the orchestration does not complete within the specified timeout period.</exception>
    public async ValueTask<TValue> GetValueAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);

        _logger.LogOrchestrationResultAwait(Orchestration, Topic);

        if (timeout.HasValue)
        {
            try
            {
                await this._completion.Task.WaitAsync(timeout.Value, cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                this._logger.LogOrchestrationResultTimeout(this.Orchestration, this.Topic);
                throw;
            }
        }

        _logger.LogOrchestrationResultComplete(Orchestration, Topic);

        return await _completion.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Cancel the orchestration associated with this result.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
    /// <remarks>
    /// Cancellation is not expected to immediately halt the orchestration.  Messages that
    /// are already in-flight may still be processed.
    /// </remarks>
    public void Cancel()
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);

        _logger.LogOrchestrationResultCancelled(Orchestration, Topic);
        _cancelSource.Cancel();
        _completion.SetCanceled();
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _cancelSource.Dispose();
            }

            _isDisposed = true;
        }
    }
}
