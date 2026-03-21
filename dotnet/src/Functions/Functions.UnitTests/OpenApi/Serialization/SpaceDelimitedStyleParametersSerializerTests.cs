// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json.Nodes;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Xunit;

namespace SemanticKernel.Functions.UnitTests.OpenApi.Serialization;

public class SpaceDelimitedStyleParametersSerializerTests
{
    [Fact]
    public void ItShouldThrowExceptionForUnsupportedParameterStyle()
    {
        // Arrange
        var parameter = new RestApiParameter("p1",
            "string",
            false,
            false,
            RestApiParameterLocation.Query,
            RestApiParameterStyle.Label);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => SpaceDelimitedStyleParameterSerializer.Serialize(parameter, "fake-argument"));
    }


    [Theory]
    [InlineData("integer")]
    [InlineData("number")]
    [InlineData("string")]
    [InlineData("boolean")]
    [InlineData("object")]
    public void ItShouldThrowExceptionIfParameterTypeIsNotArray(string parameterType)
    {
        // Arrange
        var parameter = new RestApiParameter("p1",
            parameterType,
            false,
            false,
            RestApiParameterLocation.Query,
            RestApiParameterStyle.SpaceDelimited);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => SpaceDelimitedStyleParameterSerializer.Serialize(parameter, "fake-argument"));
    }


    [Fact]
    public void ItShouldCreateAmpersandSeparatedParameterPerArrayItem()
    {
        // Arrange
        var parameter = new RestApiParameter(
            "id",
            "array",
            true,
            true, //Specifies to generate a separate parameter for each array item.
            RestApiParameterLocation.Query,
            RestApiParameterStyle.SpaceDelimited,
            "integer");

        // Act
        var result = SpaceDelimitedStyleParameterSerializer.Serialize(parameter, new JsonArray("1", "2", "3"));

        // Assert
        Assert.NotNull(result);

        Assert.Equal("id=1&id=2&id=3", result);
    }


    [Fact]
    public void ItShouldCreateParameterWithSpaceSeparatedValuePerArrayItem()
    {
        // Arrange
        var parameter = new RestApiParameter(
            "id",
            "array",
            true,
            false, //Specify generating a parameter with space-separated values for each array item.
            RestApiParameterLocation.Query,
            RestApiParameterStyle.SpaceDelimited,
            "integer");

        // Act
        var result = SpaceDelimitedStyleParameterSerializer.Serialize(parameter, new JsonArray(1, 2, 3));

        // Assert
        Assert.NotNull(result);

        Assert.Equal("id=1%202%203", result);
    }


    [Theory]
    [InlineData(":", "%3a")]
    [InlineData("/", "%2f")]
    [InlineData("?", "%3f")]
    [InlineData("#", "%23")]
    public void ItShouldEncodeSpecialSymbolsInSpaceDelimitedParameterValues(string specialSymbol, string encodedEquivalent)
    {
        // Arrange
        var parameter = new RestApiParameter("id",
            "array",
            false,
            false,
            RestApiParameterLocation.Query,
            RestApiParameterStyle.SpaceDelimited);

        // Act
        var result = SpaceDelimitedStyleParameterSerializer.Serialize(parameter, new JsonArray(specialSymbol));

        // Assert
        Assert.NotNull(result);

        Assert.EndsWith(encodedEquivalent, result, StringComparison.Ordinal);
    }


    [Theory]
    [InlineData(":", "%3a")]
    [InlineData("/", "%2f")]
    [InlineData("?", "%3f")]
    [InlineData("#", "%23")]
    public void ItShouldEncodeSpecialSymbolsInAmpersandDelimitedParameterValues(string specialSymbol, string encodedEquivalent)
    {
        // Arrange
        var parameter = new RestApiParameter("id",
            "array",
            false,
            true,
            RestApiParameterLocation.Query,
            RestApiParameterStyle.SpaceDelimited);

        // Act
        var result = SpaceDelimitedStyleParameterSerializer.Serialize(parameter, new JsonArray(specialSymbol));

        // Assert
        Assert.NotNull(result);

        Assert.EndsWith(encodedEquivalent, result, StringComparison.Ordinal);
    }
}
