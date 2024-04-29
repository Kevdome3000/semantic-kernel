// Copyright (c) Microsoft. All rights reserved.

namespace Examples;

using Microsoft.SemanticKernel.Plugins.Core;


public class MethodFunctions(ITestOutputHelper output) : BaseTest(output)
{

    [Fact]
    public Task RunAsync()
    {
        this.WriteLine("======== Functions ========");

        // Load native plugin
        var text = new TextPlugin();

        // Use function without kernel
        var result = text.Uppercase("ciao!");

        this.WriteLine(result);

        return Task.CompletedTask;
    }

}
