using AiEmployee.Application.Admin;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("/admin/debug")]
public class AdminDebugController : ControllerBase
{
    private readonly IPromptDebugService _promptDebugService;

    public AdminDebugController(IPromptDebugService promptDebugService)
    {
        _promptDebugService = promptDebugService;
    }

    [HttpGet("judge")]
    public async Task<ActionResult<PromptDebugResponse>> Judge(
        [FromQuery] Guid? botId,
        [FromQuery] string? channel,
        [FromQuery] string? externalId,
        [FromQuery] string? conversationId,
        [FromQuery] string? text,
        CancellationToken cancellationToken)
    {
        var response = await _promptDebugService.GetJudgeDebugAsync(
            botId,
            channel,
            externalId,
            conversationId,
            text,
            cancellationToken);
        return Ok(response);
    }
}
