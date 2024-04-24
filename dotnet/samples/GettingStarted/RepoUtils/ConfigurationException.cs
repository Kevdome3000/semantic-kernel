// Copyright (c) Microsoft. All rights reserved.

namespace RepoUtils;

using System;


public class ConfigurationException : Exception
{

    public ConfigurationException()
    {
    }


    public ConfigurationException(string message) : base(message)
    {
    }


    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }

}
