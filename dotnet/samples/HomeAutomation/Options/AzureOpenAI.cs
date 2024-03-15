// Copyright (c) Microsoft. All rights reserved.

namespace HomeAutomation.Options;

using System.ComponentModel.DataAnnotations;


/// <summary>
/// Azure OpenAI settings.
/// </summary>
public sealed class AzureOpenAI
{

    [Required]
    public string ChatDeploymentName { get; set; } = string.Empty;

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

}
