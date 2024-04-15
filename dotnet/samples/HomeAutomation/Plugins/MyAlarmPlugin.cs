// Copyright (c) Microsoft. All rights reserved.

namespace HomeAutomation.Plugins;

using System.ComponentModel;
using Microsoft.SemanticKernel;


/// <summary>
/// Simple plugin to illustrate creating plugins which have dependencies
/// that can be resolved through dependency injection.
/// </summary>
public class MyAlarmPlugin(MyTimePlugin timePlugin)
{

    [KernelFunction, Description("Sets an alarm at the provided time")]
    public void SetAlarm(string time)
    {
        // Code to actually set the alarm using the time plugin would be placed here
        _ = timePlugin;
    }

}
