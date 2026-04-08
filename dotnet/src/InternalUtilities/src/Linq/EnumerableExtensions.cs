// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;


[ExcludeFromCodeCoverage]
internal static class EnumerableExtensions
{
    public static IEnumerable<TSource> TakeLast<TSource>(this IEnumerable<TSource> source, int count)
    {
        Debug.Assert(source is not null);

        return Enumerable.TakeLast(source, count);
    }
}
