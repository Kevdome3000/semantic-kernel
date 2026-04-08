// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.CopilotStudio.Client;
using Microsoft.Agents.CopilotStudio.Client.Discovery;
using Microsoft.Extensions.Configuration;

namespace Microsoft.SemanticKernel.Agents.Copilot;

/// <summary>
/// <see cref="ConnectionSettings"/> with additional properties to specify Application (Client) Id,
/// Tenant Id, and optionally the Application Client secret.
/// </summary>
public sealed class CopilotStudioConnectionSettings : ConnectionSettings
{
    /// <summary>
    /// Application ID for creating the authentication for the connection
    /// </summary>
    public string AppClientId { get; }

    /// <summary>
    /// Application secret for creating the authentication for the connection
    /// </summary>
    public string? AppClientSecret { get; }

    /// <summary>
    /// Tenant ID for creating the authentication for the connection
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Use interactive or service connection for authentication.
    /// Defaults to true, meaning interactive authentication will be used.
    /// </summary>
    public bool UseInteractiveAuthentication { get; init; } = true;


    /// <summary>
    /// Instantiate a new instance of the <see cref="CopilotStudioConnectionSettings"/> from provided settings.
    /// </summary>
    public CopilotStudioConnectionSettings(string tenantId, string appClientId, string? appClientSecret = null)
    {
        TenantId = tenantId;
        AppClientId = appClientId;
        AppClientSecret = appClientSecret;
        Cloud = PowerPlatformCloud.Prod;
        CopilotAgentType = AgentType.Published;
    }


    /// <summary>
    /// Instantiate a new instance of the <see cref="CopilotStudioConnectionSettings"/> from a configuration section.
    /// </summary>
    /// <param name="config"></param>
    /// <exception cref="System.ArgumentException"></exception>
    public CopilotStudioConnectionSettings(IConfigurationSection config)
        : base(config)
    {
        AppClientId = config[nameof(AppClientId)] ?? throw new ArgumentException($"{nameof(AppClientId)} not found in config");
        TenantId = config[nameof(TenantId)] ?? throw new ArgumentException($"{nameof(TenantId)} not found in config");
        AppClientSecret = config[nameof(AppClientSecret)];
    }
}
