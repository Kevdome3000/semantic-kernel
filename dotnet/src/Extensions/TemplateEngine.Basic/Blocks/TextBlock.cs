// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.TemplateEngine.Basic.Blocks;

using Microsoft.Extensions.Logging;
using Orchestration;


internal sealed class TextBlock : Block, ITextRendering
{
    internal override BlockTypes Type => BlockTypes.Text;


    public TextBlock(string? text, ILoggerFactory? loggerFactory = null)
        : base(text, loggerFactory)
    {
    }


    public TextBlock(string text, int startIndex, int stopIndex, ILoggerFactory? loggerFactory)
        : base(text.Substring(startIndex, stopIndex - startIndex), loggerFactory)
    {
    }


    public override bool IsValid(out string errorMsg)
    {
        errorMsg = "";
        return true;
    }


    public string Render(ContextVariables? variables)
    {
        return this.Content;
    }
}
