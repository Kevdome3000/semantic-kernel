﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

/// <summary>
/// Execution serttings associated with Open AI file upload <see cref="OpenAIFileService.UploadContentAsync"/>.
/// </summary>
public sealed class OpenAIFileUploadExecutionSettings
{

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIFileUploadExecutionSettings"/> class.
    /// </summary>
    /// <param name="fileName">The file name</param>
    /// <param name="purpose">The file purpose</param>
    public OpenAIFileUploadExecutionSettings(string fileName, OpenAIFilePurpose purpose)
    {
        Verify.NotNull(fileName, nameof(fileName));

        FileName = fileName;
        Purpose = purpose;
    }


    /// <summary>
    /// The file name.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// The file purpose.
    /// </summary>
    public OpenAIFilePurpose Purpose { get; }

}
