﻿// Copyright (c) Microsoft. All rights reserved.

namespace SemanticKernel.UnitTests.Events;

using System.Globalization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Events;
using Xunit;


public class FunctionInvokedEventArgsTests
{
    [Fact]
    public void ResultValuePropertyShouldBeInitializedByOriginalOne()
    {
        //Arrange
        var originalResults = new FunctionResult(KernelFunctionFactory.CreateFromMethod(() => { }), 36, CultureInfo.InvariantCulture);

        var sut = new FunctionInvokedEventArgs(KernelFunctionFactory.CreateFromMethod(() => { }), new KernelArguments(), originalResults);

        //Assert
        Assert.Equal(36, sut.ResultValue);
    }


    [Fact]
    public void ResultValuePropertyShouldBeUpdated()
    {
        //Arrange
        var originalResults = new FunctionResult(KernelFunctionFactory.CreateFromMethod(() => { }), 36, CultureInfo.InvariantCulture);

        var sut = new FunctionInvokedEventArgs(KernelFunctionFactory.CreateFromMethod(() => { }), new KernelArguments(), originalResults);

        //Act
        sut.SetResultValue(72);

        //Assert
        Assert.Equal(72, sut.ResultValue);
    }
}