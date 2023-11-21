// Copyright (c) Microsoft. All rights reserved.

namespace RepoUtils;

using System;


public class YourAppException : Exception
{
    public YourAppException()
    {
    }


    public YourAppException(string message) : base(message)
    {
    }


    public YourAppException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
