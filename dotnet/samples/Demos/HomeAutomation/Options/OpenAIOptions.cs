// Copyright (c) Microsoft. All rights reserved.

namespace HomeAutomation.Options;

using System.ComponentModel.DataAnnotations;


/// <summary>
/// OpenAI settings.
/// </summary>
public sealed class OpenAIOptions
{

    [Required]
    public string ChatModelId { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

}
