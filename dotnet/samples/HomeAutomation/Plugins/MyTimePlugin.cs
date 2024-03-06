// Copyright (c) Microsoft. All rights reserved.

namespace HomeAutomation.Plugins;

/// <summary>
/// Simple plugin that just returns the time.
/// </summary>
public class MyTimePlugin
{

    [KernelFunction, Description("Get the current time")]
    public DateTimeOffset Time() => DateTimeOffset.Now;

}
