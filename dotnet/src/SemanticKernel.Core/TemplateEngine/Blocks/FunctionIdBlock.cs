
/* Unmerged change from project 'SemanticKernel.Core(netstandard2.0)'
Before:
// Copyright (c) Microsoft. All rights reserved.
After:
// Copyright (c) Microsoft.All rights reserved.

using System.Linq;
using System.Text.RegularExpressions;

*/
// Copyright (c) Microsoft.All rights reserved.

/* Unmerged change from project 'SemanticKernel.Core(netstandard2.0)'
Before:
using System.Linq;
using System.Text.RegularExpressions;



internal sealed partial class FunctionIdBlock : Block, ITextRendering
After:
internal sealed partial class FunctionIdBlock : Block, ITextRendering
*/

using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Microsoft.SemanticKernel.TemplateEngine;
internal sealed partial class FunctionIdBlock : Block, ITextRendering
{
    internal override BlockTypes Type => BlockTypes.FunctionId;

    internal string PluginName { get; } = string.Empty;

    internal string FunctionName { get; } = string.Empty;

    public FunctionIdBlock(string? text, ILoggerFactory? loggerFactory = null)
        : base(text?.Trim(), loggerFactory)
    {
        var functionNameParts = Content.Split('.');

        if (functionNameParts.Length > 2)
        {
            Logger.LogError("Invalid function name `{FunctionName}`.", Content);

            throw new KernelException($"Invalid function name `{Content}`. A function name can contain at most one dot separating the plugin name from the function name");
        }

        if (functionNameParts.Length == 2)
        {
            PluginName = functionNameParts[0];
            FunctionName = functionNameParts[1];

            return;
        }

        FunctionName = Content;
    }

    public override bool IsValid(out string errorMsg)
    {
        if (!ValidContentRegex().
                IsMatch(Content))
        {
            errorMsg = "The function identifier is empty";

            return false;
        }

        if (HasMoreThanOneDot(Content))
        {
            errorMsg = "The function identifier can contain max one '.' char separating plugin name from function name";

            return false;
        }

        errorMsg = "";

        return true;
    }

    /// <inheritdoc/>
    public object? Render(KernelArguments? arguments)
    {
        return Content;
    }

    private static bool HasMoreThanOneDot(string? value)
    {
        if (value is null || value.Length < 2) { return false; }

        int count = 0;

        return value.Any(t => t == '.' && ++count > 1);
    }

#if NET
    [GeneratedRegex("^[a-zA-Z0-9_.]*$")]
    private static partial Regex ValidContentRegex();
#else
    private static Regex ValidContentRegex() => s_validContentRegex;

    private static readonly Regex s_validContentRegex = new("^[a-zA-Z0-9_.]*$");
#endif

}
