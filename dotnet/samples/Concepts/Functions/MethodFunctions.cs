// Copyright (c) Microsoft. All rights reserved.

namespace Functions;

using Microsoft.SemanticKernel.Plugins.Core;


public class MethodFunctions(ITestOutputHelper output) : BaseTest(output)
{

    [Fact]
    public Task RunAsync()
    {
        Console.WriteLine("======== Functions ========");

        // Load native plugin
        var text = new TextPlugin();

        // Use function without kernel
        var result = text.Uppercase("ciao!");

        Console.WriteLine(result);

        return Task.CompletedTask;
    }

}
