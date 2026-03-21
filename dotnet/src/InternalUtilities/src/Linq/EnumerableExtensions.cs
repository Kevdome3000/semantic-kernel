// Copyright (c) Microsoft. All rights reserved.

[ExcludeFromCodeCoverage]
internal static class EnumerableExtensions
{
    public static IEnumerable<TSource> TakeLast<TSource>(this IEnumerable<TSource> source, int count)
    {
        Debug.Assert(source is not null);

#if NET || NETSTANDARD2_1_OR_GREATER
        return Enumerable.TakeLast(source, count);
#else
        return source.Skip(Math.Max(0, source.Count() - count));
#endif
    }
}
