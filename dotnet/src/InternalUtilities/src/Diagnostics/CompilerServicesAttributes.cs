﻿// Copyright (c) Microsoft. All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETCOREAPP
#pragma warning disable IDE0005 // Using directive is unnecessary.

namespace System.Runtime.CompilerServices;

using Diagnostics.CodeAnalysis;


[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class CallerArgumentExpressionAttribute : Attribute
{

    public CallerArgumentExpressionAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }


    public string ParameterName { get; }

}

#endif
