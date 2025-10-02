// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.SemanticKernel.Plugins.OpenApi;

/// <summary>
/// REST API specification.
/// </summary>
public sealed class RestApiSpecification
{

    /// <summary>
    /// The REST API information.
    /// </summary>
    public RestApiInfo Info { get; private set; }

    /// <summary>
    /// The REST API security requirements.
    /// </summary>
    public IList<RestApiSecurityRequirement>? SecurityRequirements { get; private set; }

    /// <summary>
    /// The REST API operations.
    /// </summary>
    public IList<RestApiOperation> Operations { get; private set; }


    /// <summary>
    /// Construct an instance of <see cref="RestApiSpecification"/>
    /// </summary>
    /// <param name="info">REST API information.</param>
    /// <param name="securityRequirements">REST API security requirements.</param>
    /// <param name="operations">REST API operations.</param>
    internal RestApiSpecification(RestApiInfo info, List<RestApiSecurityRequirement>? securityRequirements, IList<RestApiOperation> operations)
    {
        Info = info;
        SecurityRequirements = securityRequirements;
        Operations = operations;
    }

    internal void Freeze()
    {
        if (SecurityRequirements is not null)
        {
            SecurityRequirements = new ReadOnlyCollection<RestApiSecurityRequirement>(SecurityRequirements);
            foreach (var securityRequirement in SecurityRequirements)
            {
                securityRequirement.Freeze();
            }
        }

        Operations = new ReadOnlyCollection<RestApiOperation>(Operations);
        foreach (var operation in Operations)
        {
            operation.Freeze();
        }
    }
}
