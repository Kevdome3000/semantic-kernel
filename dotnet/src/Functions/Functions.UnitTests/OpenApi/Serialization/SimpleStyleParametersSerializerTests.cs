// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json.Nodes;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Xunit;

namespace SemanticKernel.Functions.UnitTests.OpenApi.Serialization;

public class SimpleStyleParametersSerializerTests
{
    [Fact]
    public void ItShouldCreateParameterWithCommaSeparatedValuePerArrayItem()
    {
        // Arrange
        var parameter = new RestApiParameter("id",
            "array",
            true,
            false,
            RestApiParameterLocation.Header,
            RestApiParameterStyle.Simple,
            "integer");

        // Act
        var result = SimpleStyleParameterSerializer.Serialize(parameter, new JsonArray(1, 2, 3));

        // Assert
        Assert.NotNull(result);

        Assert.Equal("1,2,3", result);
    }


    [Fact]
    public void ItShouldCreateParameterWithCommaSeparatedValuePerArrayStringItem()
    {
        // Arrange
        var parameter = new RestApiParameter("id",
            "array",
            true,
            false,
            RestApiParameterLocation.Header,
            RestApiParameterStyle.Simple,
            "integer");

        // Act
        var result = SimpleStyleParameterSerializer.Serialize(parameter, new JsonArray("1", "2", "3"));

        // Assert
        Assert.NotNull(result);

        Assert.Equal("1,2,3", result);
    }


    [Fact]
    public void ItShouldCreateParameterForPrimitiveValue()
    {
        // Arrange
        var parameter = new RestApiParameter("id",
            "integer",
            true,
            false,
            RestApiParameterLocation.Header,
            RestApiParameterStyle.Simple);

        // Act
        var result = SimpleStyleParameterSerializer.Serialize(parameter, "28");

        // Assert
        Assert.NotNull(result);

        Assert.Equal("28", result);
    }


    [Theory]
    [InlineData(":", ":")]
    [InlineData("/", "/")]
    [InlineData("?", "?")]
    [InlineData("#", "#")]
    public void ItShouldNotEncodeSpecialSymbolsInPrimitiveParameterValues(string specialSymbol, string expectedSymbol)
    {
        // Arrange
        var parameter = new RestApiParameter("id",
            "string",
            true,
            false,
            RestApiParameterLocation.Header,
            RestApiParameterStyle.Simple);

        // Act
        var result = SimpleStyleParameterSerializer.Serialize(parameter, $"fake_query_param_value{specialSymbol}");

        // Assert
        Assert.NotNull(result);

        Assert.EndsWith(expectedSymbol, result, StringComparison.Ordinal);
    }


    [Theory]
    [InlineData(":", ":")]
    [InlineData("/", "/")]
    [InlineData("?", "?")]
    [InlineData("#", "#")]
    public void ItShouldEncodeSpecialSymbolsInCommaSeparatedParameterValues(string specialSymbol, string expectedSymbol)
    {
        // Arrange
        var parameter = new RestApiParameter("id",
            "array",
            true,
            false,
            RestApiParameterLocation.Header,
            RestApiParameterStyle.Simple);

        // Act
        var result = SimpleStyleParameterSerializer.Serialize(parameter, new JsonArray(specialSymbol));

        // Assert
        Assert.NotNull(result);

        Assert.EndsWith(expectedSymbol, result, StringComparison.Ordinal);
    }
}
