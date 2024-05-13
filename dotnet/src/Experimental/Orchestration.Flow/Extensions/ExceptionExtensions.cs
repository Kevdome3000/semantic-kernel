// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Experimental.Orchestration.Extensions;

using System;
using System.Net;


internal static class ExceptionExtensions
{

    internal static bool IsNonRetryable(this Exception ex)
    {
        bool isContentFilterException = ex is HttpOperationException
        {
            StatusCode: HttpStatusCode.BadRequest, InnerException: { }
        } hoe && hoe.InnerException?.Message.Contains("content_filter") is true;

        return isContentFilterException || ex.IsCriticalException();
    }

}
