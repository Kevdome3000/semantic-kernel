// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.SemanticKernel.Plugins.MsGraph.Diagnostics;

namespace Microsoft.SemanticKernel.Plugins.MsGraph;

/// <summary>
/// Cloud drive plugin (e.g. OneDrive).
/// </summary>
public sealed class CloudDrivePlugin
{
    private readonly ICloudDriveConnector _connector;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudDrivePlugin"/> class.
    /// </summary>
    /// <param name="connector">The cloud drive connector.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public CloudDrivePlugin(ICloudDriveConnector connector, ILoggerFactory? loggerFactory = null)
    {
        Ensure.NotNull(connector, nameof(connector));

        _connector = connector;
        _logger = loggerFactory?.CreateLogger(typeof(CloudDrivePlugin)) ?? NullLogger.Instance;
    }

    /// <summary>
    /// Get the contents of a file stored in a cloud drive.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A string containing the file content.</returns>
    [KernelFunction] [Description("Get the contents of a file in a cloud drive.")]
    public async Task<string?> GetFileContentAsync(
        [Description("Path to file")] string filePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting file content for '{0}'", filePath);
        Stream? fileContentStream = await _connector.GetFileContentStreamAsync(filePath, cancellationToken).ConfigureAwait(false);

        if (fileContentStream is null)
        {
            _logger.LogDebug("File content stream for '{0}' is null", filePath);
            return null;
        }

        using StreamReader sr = new(fileContentStream);
        return await sr.ReadToEndAsync(
            cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Upload a small file to OneDrive (less than 4MB).
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="destinationPath">The remote path to store the file.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    [KernelFunction] [Description("Upload a small file to OneDrive (less than 4MB).")]
    public async Task UploadFileAsync(
        [Description("Path to file")] string filePath,
        [Description("Remote path to store the file")]
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("Variable was null or whitespace", nameof(destinationPath));
        }

        _logger.LogDebug("Uploading file '{0}'", filePath);

        // TODO Add support for large file uploads (i.e. upload sessions)
        await _connector.UploadSmallFileAsync(filePath, destinationPath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Create a sharable link to a file stored in a cloud drive.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A string containing the sharable link.</returns>
    [KernelFunction] [Description("Create a sharable link to a file stored in a cloud drive.")]
    public async Task<string> CreateLinkAsync(
        [Description("Path to file")] string filePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating link for '{0}'", filePath);
        const string Type = "view"; // TODO expose this as an SK variable
        const string Scope = "anonymous"; // TODO expose this as an SK variable

        return await _connector.CreateShareLinkAsync(filePath,
                Type,
                Scope,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
