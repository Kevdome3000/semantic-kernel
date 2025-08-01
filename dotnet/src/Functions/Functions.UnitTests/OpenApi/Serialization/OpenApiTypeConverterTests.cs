﻿// Copyright (c) Microsoft. All rights reserved.

namespace SemanticKernel.Functions.UnitTests.OpenApi.Serialization;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Microsoft.VisualBasic;
using Xunit;


public class OpenApiTypeConverterTests
{
    [Fact]
    public void ItShouldConvertString()
    {
        // Act & Assert
        Assert.Equal("\"test\"", OpenApiTypeConverter.Convert("id", "string", "test").ToString());
        Assert.Equal("test", OpenApiTypeConverter.Convert("id", "string", CreateJsonElement("test")).ToString());
    }


    [Fact]
    public void ItShouldConvertNumber()
    {
        // Act & Assert - Basic numeric types
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "number", (byte)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "number", (sbyte)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "number", (short)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "number", (ushort)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "number", (int)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "number", (uint)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "number", (long)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "number", (ulong)10).ToString());
        Assert.Equal("10.5", OpenApiTypeConverter.Convert("id", "number", (float)10.5).ToString());
        Assert.Equal("10.5", OpenApiTypeConverter.Convert("id", "number", (double)10.5).ToString());
        Assert.Equal("10.5", OpenApiTypeConverter.Convert("id", "number", (decimal)10.5).ToString());

        // String conversions
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "number", "10").ToString());
        Assert.Equal("10.5", OpenApiTypeConverter.Convert("id", "number", "10.5").ToString());

        // JsonElement conversions
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "number", CreateJsonElement(10)).ToString());
        Assert.Equal("10.5", OpenApiTypeConverter.Convert("id", "number", CreateJsonElement(10.5)).ToString());
    }


    [Fact]
    public void ItShouldConvertInteger()
    {
        // Act & Assert - Basic integer types
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "integer", (byte)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "integer", (sbyte)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "integer", (short)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "integer", (ushort)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "integer", (int)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "integer", (uint)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "integer", (long)10).ToString());
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "integer", (ulong)10).ToString());

        // String conversion
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "integer", "10").ToString());

        // JsonElement conversion
        Assert.Equal("10", OpenApiTypeConverter.Convert("id", "integer", CreateJsonElement(10)).ToString());
    }


    [Fact]
    public void ItShouldConvertBoolean()
    {
        // Act & Assert - Basic boolean values
        Assert.Equal("true", OpenApiTypeConverter.Convert("id", "boolean", true).ToString());
        Assert.Equal("false", OpenApiTypeConverter.Convert("id", "boolean", false).ToString());

        // String conversions
        Assert.Equal("true", OpenApiTypeConverter.Convert("id", "boolean", "true").ToString());
        Assert.Equal("false", OpenApiTypeConverter.Convert("id", "boolean", "false").ToString());

        // JsonElement conversions
        Assert.Equal("true", OpenApiTypeConverter.Convert("id", "boolean", CreateJsonElement(true)).ToString());
        Assert.Equal("false", OpenApiTypeConverter.Convert("id", "boolean", CreateJsonElement(false)).ToString());
    }


    [Fact]
    public void ItShouldConvertDateTime()
    {
        // Arrange
        var dateTime = DateTime.ParseExact("06.12.2023 11:53:36+02:00", "dd.MM.yyyy HH:mm:sszzz", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

        // Act & Assert
        Assert.Equal("\"2023-12-06T09:53:36Z\"", OpenApiTypeConverter.Convert("id", "string", dateTime).ToString());
    }


    [Fact]
    public void ItShouldConvertDateTimeOffset()
    {
        // Arrange
        var offset = DateTimeOffset.ParseExact("06.12.2023 11:53:36 +02:00", "dd.MM.yyyy HH:mm:ss zzz", CultureInfo.InvariantCulture);

        // Act & Assert
        Assert.Equal("\"2023-12-06T11:53:36+02:00\"", OpenApiTypeConverter.Convert("id", "string", offset).ToString());
    }


    [Fact]
    public void ItShouldConvertCollections()
    {
        // Act & Assert - Basic collections
        Assert.Equal("[1,2,3]", OpenApiTypeConverter.Convert("id", "array", new[] { 1, 2, 3 }).ToJsonString());
        Assert.Equal("[1,2,3]", OpenApiTypeConverter.Convert("id", "array", new List<int> { 1, 2, 3 }).ToJsonString());
        Assert.Equal("[1,2,3]", OpenApiTypeConverter.Convert("id", "array", new Collection() { 1, 2, 3 }).ToJsonString());
        Assert.Equal("[1,2,3]", OpenApiTypeConverter.Convert("id", "array", "[1, 2, 3]").ToJsonString());

        // JsonElement array conversion
        Assert.Equal("[1,2,3]", OpenApiTypeConverter.Convert("id", "array", CreateJsonElement(new[] { 1, 2, 3 })).ToJsonString());
    }

    [Fact]
    public void ItShouldConvertWithNoTypeAndNoSchema()
    {
        // Act
        var result = OpenApiTypeConverter.Convert("lat", null!, 51.8985136);

        // Assert
        Assert.Equal(51.8985136, result.GetValue<double>());
    }

    [Fact]
    public void ItShouldConvertWithNoTypeAndValidSchema()
    {
        // Arrange
        var schema = KernelJsonSchema.Parse(
        """
        {
            "type": "number",
            "format": "double",
            "nullable": false
        }
        """);

        // Act
        var result = OpenApiTypeConverter.Convert("lat", null!, 51.8985136, schema);

        // Assert
        Assert.Equal(51.8985136, result.GetValue<double>());
    }

    [Fact]
    public void ItShouldThrowExceptionWhenNoTypeAndInvalidSchema()
    {
        // Arrange
        var schema = KernelJsonSchema.Parse(
        """
        {
            "type": "boolean",
            "nullable": false
        }
        """);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => OpenApiTypeConverter.Convert("lat", null!, 51.8985136, schema));
    }

    private static JsonElement CreateJsonElement(object value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<JsonElement>(json)!;
    }
}
