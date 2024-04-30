// Copyright (c) Microsoft. All rights reserved.

namespace Search;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Web;


public class WebSearchQueriesPlugin(ITestOutputHelper output) : BaseTest(output)
{

    [Fact]
    public async Task RunAsync()
    {
        Console.WriteLine("======== WebSearchQueries ========");

        Kernel kernel = new();

        // Load native plugins
        var bing = kernel.ImportPluginFromType<SearchUrlPlugin>("search");

        // Run
        var ask = "What's the tallest building in Europe?";
        var result = await kernel.InvokeAsync(bing["BingSearchUrl"], new() { ["query"] = ask });

        Console.WriteLine(ask + "\n");
        Console.WriteLine(result.GetValue<string>());

        /* Expected output:
         * ======== WebSearchQueries ========
         * What's the tallest building in Europe?
         *
         * https://www.bing.com/search?q=What%27s%20the%20tallest%20building%20in%20Europe%3F
         * == DONE ==
         */
    }

}
