// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.CopilotStudio.Client;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Microsoft.SemanticKernel.Agents.Copilot;

/// <summary>
/// A <see cref="DelegatingHandler"/> that adds an authentication token to the request headers for Copilot Studio API calls.
/// </summary>
/// <remarks>
///  For more information on how to setup various authentication flows, see the Microsoft Identity documentation at https://aka.ms/msal.
/// </remarks>
internal sealed class CopilotStudioTokenHandler : DelegatingHandler
{
    private const string AuthenticationHeader = "Bearer";
    private const string CacheFolderName = "mcs_client_console";
    private const string KeyChainServiceName = "copilot_studio_client_app";
    private const string KeyChainAccountName = "copilot_studio_client";

    private readonly CopilotStudioConnectionSettings _settings;
    private readonly string[] _scopes;

    private IConfidentialClientApplication? _clientApplication;


    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotStudioTokenHandler"/> class with the specified connection settings.`
    /// </summary>
    /// <param name="settings">The connection settings for Copilot Studio.</param>
    public CopilotStudioTokenHandler(CopilotStudioConnectionSettings settings)
    {
        Verify.NotNull(settings, nameof(settings));

        _settings = settings;
        _scopes = [CopilotClient.ScopeFromSettings(_settings)];
        InnerHandler = new HttpClientHandler();
    }


    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            AuthenticationResult authResponse = await AuthenticateAsync(cancellationToken).ConfigureAwait(false);

            request.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationHeader, authResponse.AccessToken);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }


    private Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken)
    {
        return _settings.UseInteractiveAuthentication
            ? AuthenticateInteractiveAsync(cancellationToken)
            : AuthenticateServiceAsync(cancellationToken);
    }


    private async Task<AuthenticationResult> AuthenticateServiceAsync(CancellationToken cancellationToken)
    {
        if (_clientApplication is null)
        {
            _clientApplication = ConfidentialClientApplicationBuilder.Create(_settings.AppClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, _settings.TenantId)
                .WithClientSecret(_settings.AppClientSecret)
                .Build();

            MsalCacheHelper tokenCacheHelper = await CreateCacheHelper("AppTokenCache").ConfigureAwait(false);
            tokenCacheHelper.RegisterCache(_clientApplication.AppTokenCache);
        }

        AuthenticationResult authResponse;

        authResponse = await _clientApplication.AcquireTokenForClient(_scopes).ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return authResponse;
    }


    private async Task<AuthenticationResult> AuthenticateInteractiveAsync(CancellationToken cancellationToken = default!)
    {
        IPublicClientApplication app =
            PublicClientApplicationBuilder.Create(_settings.AppClientId)
                .WithAuthority(AadAuthorityAudience.AzureAdMyOrg)
                .WithTenantId(_settings.TenantId)
                .WithRedirectUri("http://localhost")
                .Build();

        MsalCacheHelper tokenCacheHelper = await CreateCacheHelper("TokenCache").ConfigureAwait(false);
        tokenCacheHelper.RegisterCache(app.UserTokenCache);

        IEnumerable<IAccount> accounts = await app.GetAccountsAsync().ConfigureAwait(false);
        IAccount? account = accounts.FirstOrDefault();

        AuthenticationResult authResponse;

        try
        {
            authResponse = await app.AcquireTokenSilent(_scopes, account).ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (MsalUiRequiredException)
        {
            authResponse = await app.AcquireTokenInteractive(_scopes).ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }

        return authResponse;
    }


    private static async Task<MsalCacheHelper> CreateCacheHelper(string cacheFileName)
    {
        string currentDir = Path.Combine(AppContext.BaseDirectory, CacheFolderName);

        if (!Directory.Exists(currentDir))
        {
            Directory.CreateDirectory(currentDir);
        }

        StorageCreationPropertiesBuilder storageProperties = new(cacheFileName, currentDir);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            storageProperties.WithLinuxUnprotectedFile();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            storageProperties.WithMacKeyChain(KeyChainServiceName, KeyChainAccountName);
        }

        MsalCacheHelper tokenCacheHelper = await MsalCacheHelper.CreateAsync(storageProperties.Build()).ConfigureAwait(false);

        return tokenCacheHelper;
    }
}
