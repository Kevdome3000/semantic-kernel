// Copyright (c) Microsoft. All rights reserved.

#if !NET5_0_OR_GREATER
namespace System.Net.Http;

[ExcludeFromCodeCoverage]
internal static class HttpContentPolyfills
{
    internal static Task<string> ReadAsStringAsync(this HttpContent httpContent, CancellationToken cancellationToken)
    {
        return httpContent.ReadAsStringAsync();
    }


    internal static Task<Stream> ReadAsStreamAsync(this HttpContent httpContent, CancellationToken cancellationToken)
    {
        return httpContent.ReadAsStreamAsync();
    }


    internal static Task<byte[]> ReadAsByteArrayAsync(this HttpContent httpContent, CancellationToken cancellationToken)
    {
        return httpContent.ReadAsByteArrayAsync();
    }
}

#endif
