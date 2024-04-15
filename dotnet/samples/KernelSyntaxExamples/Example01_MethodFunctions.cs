// Copyright (c) Microsoft. All rights reserved.

namespace Examples;

using System.Threading.Tasks;
using Microsoft.SemanticKernel.Plugins.Core;
using Xunit;
using Xunit.Abstractions;


public class Example01_MethodFunctions(ITestOutputHelper output) : BaseTest(output)
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
