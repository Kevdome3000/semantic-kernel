// Copyright (c) Microsoft. All rights reserved.

using static Microsoft.SemanticKernel.Plugins.Core.CodeInterpreter.SessionsPythonSettings;

namespace Microsoft.SemanticKernel.Plugins.Core.CodeInterpreter;

using System.Text.Json.Serialization;


internal sealed class SessionsPythonCodeExecutionProperties
{

    /// <summary>
    /// Code input type.
    /// </summary>
    [JsonPropertyName("codeInputType")]
    public CodeInputTypeSetting CodeInputType { get; } = CodeInputTypeSetting.Inline;

    /// <summary>
    /// Code execution type.
    /// </summary>
    [JsonPropertyName("executionType")]
    public CodeExecutionTypeSetting CodeExecutionType { get; } = CodeExecutionTypeSetting.Synchronous;

    /// <summary>
    /// Timeout in seconds for the code execution.
    /// </summary>
    [JsonPropertyName("timeoutInSeconds")]
    public int TimeoutInSeconds { get; } = 100;

    /// <summary>
    /// The Python code to execute.
    /// </summary>
    [JsonPropertyName("code")]
    public string PythonCode { get; }


    public SessionsPythonCodeExecutionProperties(SessionsPythonSettings settings, string pythonCode)
    {
        PythonCode = pythonCode;
        TimeoutInSeconds = settings.TimeoutInSeconds;
        CodeInputType = settings.CodeInputType;
        CodeExecutionType = settings.CodeExecutionType;
    }

}
