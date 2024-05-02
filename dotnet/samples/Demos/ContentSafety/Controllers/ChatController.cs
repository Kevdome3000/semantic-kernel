// Copyright (c) Microsoft. All rights reserved.

namespace ContentSafety.Controllers;

using Microsoft.AspNetCore.Mvc;
using Models;


/// <summary>
/// Sample chat controller.
/// </summary>
[ApiController]
[Route("[controller]")]
public class ChatController(Kernel kernel) : ControllerBase
{

    private const string Prompt =
        """
        <message role="system">You are friendly assistant.</message>
        <message role="user">{{$userMessage}}</message>
        """;

    private readonly Kernel _kernel = kernel;


    [HttpPost]
    public async Task<IActionResult> PostAsync(ChatModel chat)
    {
        var arguments = new KernelArguments
        {
            ["userMessage"] = chat.Message,
            ["documents"] = chat.Documents
        };

        return this.Ok((await this._kernel.InvokePromptAsync(Prompt, arguments)).ToString());
    }

}
