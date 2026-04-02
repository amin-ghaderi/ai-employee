using AiEmployee.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("telegram")]
public class TelegramController : ControllerBase
{
    private readonly JudgeUseCase _judgeUseCase;

    public TelegramController(JudgeUseCase judgeUseCase)
    {
        _judgeUseCase = judgeUseCase;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramWebhookRequest request)
    {
        var judgment = await _judgeUseCase.Execute(request.UserId, request.Text);
        return Ok(new { winner = judgment.Winner, reason = judgment.Reason });
    }
}

public sealed class TelegramWebhookRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
