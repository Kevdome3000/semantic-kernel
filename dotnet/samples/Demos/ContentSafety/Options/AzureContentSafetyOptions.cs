// Copyright (c) Microsoft. All rights reserved.

namespace ContentSafety.Options;

using System.ComponentModel.DataAnnotations;


/// <summary>
/// Configuration for Azure AI Content Safety service.
/// </summary>
public class AzureContentSafetyOptions
{

    public const string SectionName = "AzureContentSafety";

    [Required]
    public string Endpoint { get; set; }

    [Required]
    public string ApiKey { get; set; }

}
