// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Plugins.MsGraph;

/// <summary>
/// Interface for organization hierarchy connections (e.g. Azure AD).
/// </summary>
public interface IOrganizationHierarchyConnector
{
    /// <summary>
    /// Get the user's direct reports' email addresses.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The user's direct reports' email addresses.</returns>
    Task<IEnumerable<string>?> GetDirectReportsEmailAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// Get the user's manager's email address.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The user's manager's email address.</returns>
    Task<string?> GetManagerEmailAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// Get the user's manager's name.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The user's manager's name.</returns>
    Task<string?> GetManagerNameAsync(CancellationToken cancellationToken = default);
}
