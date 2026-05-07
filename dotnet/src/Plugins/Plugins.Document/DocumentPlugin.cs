// Copyright (c) Microsoft.All rights reserved.

using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.Plugins.Document.FileSystem;

namespace Microsoft.SemanticKernel.Plugins.Document;

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
/// <remarks>
/// <para>
/// This plugin is secure by default. <see cref="AllowedDirectories"/> must be explicitly configured
/// before any file operations are permitted. By default, all file paths are denied.
/// </para>
/// <para>
/// When exposing this plugin to an LLM via auto function calling, ensure that
/// <see cref="AllowedDirectories"/> is restricted to trusted values only.
/// </para>
/// </remarks>
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
    /// List of allowed directories for file operations. Subdirectories of allowed directories are also permitted.
    /// </summary>
    /// <remarks>
    /// Defaults to an empty collection (no directories allowed). Must be explicitly populated
    /// with trusted directory paths before any file operations will succeed.
    /// Paths are canonicalized before validation to prevent directory traversal.
    /// </remarks>
    public IEnumerable<string>? AllowedDirectories
    {
        get => _allowedDirectories;
        set => _allowedDirectories = value is null
            ? null
            : new HashSet<string>(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Read all text from a document, using the filePath argument as the file path.
    /// </summary>
    [KernelFunction]
    [Description("Read all text from a document")]
    public async Task<string> ReadTextAsync(
        [Description("Path to the file to read")]
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var canonicalPath = CanonicalizePath(filePath);

        _logger.LogDebug("Reading text from {0}", canonicalPath);

        if (!IsFilePathAllowed(canonicalPath))
        {
            throw new InvalidOperationException("Reading from the provided location is not allowed.");
        }

        using var stream = await _fileSystemConnector.GetFileContentStreamAsync(canonicalPath, cancellationToken).ConfigureAwait(false);
        return _documentConnector.ReadText(stream);
    }

    /// <summary>
    /// Append the text specified by the text argument to a document. If the document doesn't exist, it will be created.
    /// </summary>
    [KernelFunction]
    [Description("Append text to a document. If the document doesn't exist, it will be created.")]
    public async Task AppendTextAsync(
        [Description("Text to append")] string text,
        [Description("Destination file path")] string filePath,
        CancellationToken cancellationToken = default)
    {
        var canonicalPath = CanonicalizePath(filePath);

        if (!IsFilePathAllowed(canonicalPath))
        {
            throw new InvalidOperationException("Writing to the provided location is not allowed.");
        }

        // If the document already exists, open it. If not, create it.
        if (await _fileSystemConnector.FileExistsAsync(canonicalPath, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug("Writing text to file {0}", canonicalPath);
            using Stream stream = await _fileSystemConnector.GetWriteableFileStreamAsync(canonicalPath, cancellationToken).ConfigureAwait(false);
            _documentConnector.AppendText(stream, text);
        }
        else
        {
            _logger.LogDebug("File does not exist. Creating file at {0}", canonicalPath);
            using Stream stream = await _fileSystemConnector.CreateFileAsync(canonicalPath, cancellationToken).ConfigureAwait(false);
            _documentConnector.Initialize(stream);

            _logger.LogDebug("Writing text to {0}", canonicalPath);
            _documentConnector.AppendText(stream, text);
        }
    }

    #region private

    private HashSet<string>? _allowedDirectories = [];

    /// <summary>
    /// Expands environment variables and resolves the path to its canonical form.
    /// This must be called before validation to prevent validate/use mismatches.
    /// </summary>
    private static string CanonicalizePath(string path)
    {
        Verify.NotNullOrWhiteSpace(path);

        if (path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid file path, UNC paths are not supported.", nameof(path));
        }

        // Expand environment variables first, then canonicalize — so that
        // validation and I/O operate on the same resolved path.
        var expanded = Environment.ExpandEnvironmentVariables(path);

        // Re-check after expansion: an env var could have expanded to a UNC
        // or extended-path prefix (e.g., %NETSHARE% → \\server\share).
        if (expanded.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid file path, UNC paths are not supported.", nameof(path));
        }

        return Path.GetFullPath(expanded);
    }

    // Use case-insensitive comparison on Windows (case-insensitive FS), case-sensitive on Linux/macOS.
    private static readonly StringComparison s_pathComparison =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    /// <summary>
    /// Checks whether a canonicalized file path falls within one of the allowed directories.
    /// Subdirectories of allowed directories are also permitted.
    /// </summary>
    private bool IsFilePathAllowed(string canonicalPath)
    {
        string? directoryPath = Path.GetDirectoryName(canonicalPath);

        if (string.IsNullOrEmpty(directoryPath))
        {
            throw new ArgumentException("Invalid file path, a fully qualified file location must be specified.", nameof(canonicalPath));
        }

        if (_allowedDirectories is null || _allowedDirectories.Count == 0)
        {
            return false;
        }

        foreach (var allowedDirectory in _allowedDirectories)
        {
            var canonicalAllowed = Path.GetFullPath(allowedDirectory);
            var separator = Path.DirectorySeparatorChar.ToString();

            if (!canonicalAllowed.EndsWith(separator, s_pathComparison))
            {
                canonicalAllowed += separator;
            }

            if (directoryPath.StartsWith(canonicalAllowed, s_pathComparison)
                || (directoryPath + separator).Equals(canonicalAllowed, s_pathComparison))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

}
