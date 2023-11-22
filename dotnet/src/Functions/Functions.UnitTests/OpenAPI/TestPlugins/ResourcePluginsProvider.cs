// Copyright (c) Microsoft. All rights reserved.

namespace SemanticKernel.Functions.UnitTests.OpenAPI.TestPlugins;

using System.IO;
using System.Resources;


internal static class ResourcePluginsProvider
{
    /// <summary>
    /// Loads OpenAPI document from assembly resource.
    /// </summary>
    /// <param name="resourceName">The resource name.</param>
    /// <returns>The OpenAPI document resource stream.</returns>
    public static Stream LoadFromResource(string resourceName)
    {
        var type = typeof(ResourcePluginsProvider);

        var stream = type.Assembly.GetManifestResourceStream(type, resourceName);

        if (stream == null)
        {
            throw new MissingManifestResourceException($"Unable to load OpenAPI plugin from assembly resource '{resourceName}'.");
        }

        return stream;
    }
}
