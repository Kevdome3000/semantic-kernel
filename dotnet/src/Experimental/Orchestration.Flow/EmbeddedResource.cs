﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Experimental.Orchestration;

using System.IO;
using System.Reflection;
using Diagnostics;


internal static class EmbeddedResource
{
    private static readonly string? s_namespace = typeof(EmbeddedResource).Namespace;


    internal static string? Read(string name, bool throwIfNotFound = true)
    {
        var assembly = typeof(EmbeddedResource).GetTypeInfo().Assembly;

        if (assembly is null) { throw new SKException($"[{s_namespace}] {name} assembly not found"); }

        using Stream? resource = assembly.GetManifestResourceStream($"{s_namespace}." + name);

        if (resource is null)
        {
            if (!throwIfNotFound)
            {
                return null;
            }

            throw new SKException($"[{s_namespace}] {name} resource not found");
        }

        using var reader = new StreamReader(resource);
        return reader.ReadToEnd();
    }
}