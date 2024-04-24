// Copyright (c) Microsoft. All rights reserved.
namespace SemanticKernel.UnitTests.Contents;

using Microsoft.SemanticKernel;
using Xunit;

#pragma warning disable SKEXP0110


/// <summary>
/// Unit testing of <see cref="FileReferenceContent"/>.
/// </summary>
public class FileReferenceContentTests
{

    /// <summary>
    /// Verify default state.
    /// </summary>
    [Fact]
    public void VerifyFileReferenceContentInitialState()
    {
        FileReferenceContent definition = new();

        Assert.Empty(definition.FileId);
    }


    /// <summary>
    /// Verify usage.
    /// </summary>
    [Fact]
    public void VerifyFileReferenceContentUsage()
    {
        FileReferenceContent definition = new(fileId: "testfile");

        Assert.Equal("testfile", definition.FileId);
    }

}
