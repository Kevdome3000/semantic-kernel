// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.SemanticKernel.Planning.Stepwise.UnitTests;

using Moq;
using Services;
using Xunit;

#pragma warning restore IDE0130 // Namespace does not match folder structure


public sealed class StepwisePlannerTests
{
    [Fact]
    public void UsesPromptDelegateWhenProvided()
    {
        // Arrange
        var kernel = new Kernel(new Mock<IAIServiceProvider>().Object);

        var getPromptTemplateMock = new Mock<Func<string>>();
        var config = new StepwisePlannerConfig()
        {
            GetPromptTemplate = getPromptTemplateMock.Object
        };

        // Act
        var planner = new StepwisePlanner(kernel, config);

        // Assert
        getPromptTemplateMock.Verify(x => x(), Times.Once());
    }
}
