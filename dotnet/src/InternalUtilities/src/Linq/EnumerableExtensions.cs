// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

[ExcludeFromCodeCoverage]
internal static class EnumerableExtensions
{
    public static IEnumerable<TSource> TakeLast<TSource>(this IEnumerable<TSource> source, int count)
    {
        Debug.Assert(source is not null);

        return Enumerable.TakeLast(source, count);
    }
}
