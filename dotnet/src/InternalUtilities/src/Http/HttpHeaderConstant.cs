﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Http;

using System;
using System.Diagnostics.CodeAnalysis;


/// <summary>Provides HTTP header names and values for common purposes.</summary>
[ExcludeFromCodeCoverage]
internal static class HttpHeaderConstant
{

    public static class Names
    {

        /// <summary>HTTP header name to use to include the Semantic Kernel package version in all HTTP requests issued by Semantic Kernel.</summary>
        public static string SemanticKernelVersion => "Semantic-Kernel-Version";

    }


    public static class Values
    {

        /// <summary>User agent string to use for all HTTP requests issued by Semantic Kernel.</summary>
        public static string UserAgent => "Semantic-Kernel";


        /// <summary>
        /// Gets the version of the <see cref="System.Reflection.Assembly"/> in which the specific type is declared.
        /// </summary>
        /// <param name="type">Type for which the assembly version is returned.</param>
        public static string GetAssemblyVersion(Type type)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference. Impacts Milvus connector package because it targets net6.0 and netstandard2.0
            return type.Assembly.GetName().
                Version.ToString();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

    }

}