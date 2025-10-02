// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Plugins.Document;

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Logging;
using Extensions.Logging.Abstractions;
using FileSystem;

//**********************************************************************************************************************
// EXAMPLE USAGE
// Option #1: as a standalone C# function
//
// DocumentPlugin documentPlugin = new(new WordDocumentConnector(), new LocalDriveConnector());
// string filePath = "PATH_TO_DOCX_FILE.docx";
// string text = await documentPlugin.ReadTextAsync(filePath);
// Console.WriteLine(text);
//
//
// Option #2: with the Semantic Kernel
//
// DocumentPlugin documentPlugin = new(new WordDocumentConnector(), new LocalDriveConnector());
// string filePath = "PATH_TO_DOCX_FILE.docx";
// ISemanticKernel kernel = SemanticKernel.Build();
// var result = await kernel.RunAsync(
//      filePath,
//      documentPlugin.ReadTextAsync);
// Console.WriteLine(result);
//**********************************************************************************************************************


/// <summary>
/// Plugin for interacting with documents (e.g. Microsoft Word)
/// </summary>
public sealed class DocumentPlugin
{
    private readonly IDocumentConnector _documentConnector;
    private readonly IFileSystemConnector _fileSystemConnector;
    private readonly ILogger _logger;


    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentPlugin"/> class.
    /// </summary>
    /// <param name="documentConnector">Document connector</param>
    /// <param name="fileSystemConnector">File system connector</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public DocumentPlugin(IDocumentConnector documentConnector, IFileSystemConnector fileSystemConnector, ILoggerFactory? loggerFactory = null)
    {
        _documentConnector = documentConnector ?? throw new ArgumentNullException(nameof(documentConnector));
        _fileSystemConnector = fileSystemConnector ?? throw new ArgumentNullException(nameof(fileSystemConnector));
        _logger = loggerFactory?.CreateLogger(typeof(DocumentPlugin)) ?? NullLogger.Instance;
    }


    /// <summary>
    /// Read all text from a document, using the filePath argument as the file path.
    /// </summary>
    [KernelFunction, Description("Read all text from a document")]
    public async Task<string> ReadTextAsync(
        [Description("Path to the file to read")]
        string filePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading text from {0}", filePath);
        using var stream = await _fileSystemConnector.GetFileContentStreamAsync(filePath, cancellationToken).ConfigureAwait(false);
        return _documentConnector.ReadText(stream);
    }


    /// <summary>
    /// Append the text specified by the text argument to a document. If the document doesn't exist, it will be created.
    /// </summary>
    [KernelFunction, Description("Append text to a document. If the document doesn't exist, it will be created.")]
    public async Task AppendTextAsync(
        [Description("Text to append")] string text,
        [Description("Destination file path")] string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(@"Variable was null or whitespace", nameof(filePath));
        }

        // If the document already exists, open it. If not, create it.
        if (await _fileSystemConnector.FileExistsAsync(filePath, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug("Writing text to file {0}", filePath);
            using Stream stream = await _fileSystemConnector.GetWriteableFileStreamAsync(filePath, cancellationToken).ConfigureAwait(false);
            _documentConnector.AppendText(stream, text);
        }
        else
        {
            _logger.LogDebug("File does not exist. Creating file at {0}", filePath);
            using Stream stream = await _fileSystemConnector.CreateFileAsync(filePath, cancellationToken).ConfigureAwait(false);
            _documentConnector.Initialize(stream);

            _logger.LogDebug("Writing text to {0}", filePath);
            _documentConnector.AppendText(stream, text);
        }
    }
}
