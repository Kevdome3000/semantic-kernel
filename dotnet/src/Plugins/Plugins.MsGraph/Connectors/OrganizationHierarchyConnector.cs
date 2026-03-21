// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Plugins.MsGraph.Connectors;

/// <summary>
/// Connector for Microsoft Graph API for organizational hierarchy.
/// </summary>
public class OrganizationHierarchyConnector : IOrganizationHierarchyConnector
{
    private readonly GraphServiceClient _graphServiceClient;


    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationHierarchyConnector"/> class.
    /// </summary>
    /// <param name="graphServiceClient">A graph service client.</param>
    public OrganizationHierarchyConnector(GraphServiceClient graphServiceClient)
    {
        _graphServiceClient = graphServiceClient;
    }


    /// <inheritdoc/>
    public async Task<string?> GetManagerEmailAsync(CancellationToken cancellationToken = default)
    {
        return ((User?)await _graphServiceClient.Me
            .Manager.GetAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false))?.UserPrincipalName;
    }


    /// <inheritdoc/>
    public async Task<string?> GetManagerNameAsync(CancellationToken cancellationToken = default)
    {
        return ((User?)await _graphServiceClient.Me
            .Manager.GetAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false))?.DisplayName;
    }


    /// <inheritdoc/>
    public async Task<IEnumerable<string>?> GetDirectReportsEmailAsync(CancellationToken cancellationToken = default)
    {
        DirectoryObjectCollectionResponse? directsPage = await _graphServiceClient.Me
            .DirectReports.GetAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        List<User>? directs = directsPage?.Value?.Cast<User>().ToList();

        while (directs is { Count: > 0 } && directsPage!.OdataNextLink is not null)
        {
            directsPage = await _graphServiceClient.Me.DirectReports.GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            if (directsPage?.Value is not null)
            {
                directs.AddRange(directsPage!.Value.Cast<User>());
            }
        }

        return directs?.Where(d => d.UserPrincipalName is not null)?.Select(d => d.UserPrincipalName!);
    }
}
