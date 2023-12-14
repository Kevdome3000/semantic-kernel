// Copyright (c) Microsoft. All rights reserved.

namespace SemanticKernel.IntegrationTests.TestSettings;

using System.Diagnostics.CodeAnalysis;


[SuppressMessage("Performance", "CA1812:Internal class that is apparently never instantiated",
    Justification = "Configuration classes are instantiated through IConfiguration.")]
internal sealed class PlannerConfiguration
{
    public AzureOpenAIConfiguration? AzureOpenAI { get; set; }

    public OpenAIConfiguration? OpenAI { get; set; }


    public PlannerConfiguration(AzureOpenAIConfiguration? azureOpenAIConfiguration, OpenAIConfiguration? openAIConfiguration)
    {
        this.AzureOpenAI = azureOpenAIConfiguration;
        this.OpenAI = openAIConfiguration;
    }
}
