using AiEmployee.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("telegram")]
public class TelegramController : ControllerBase
{
    private readonly HandleMessageUseCase _handleMessageUseCase;

    public TelegramController(HandleMessageUseCase handleMessageUseCase)
    {
        _handleMessageUseCase = handleMessageUseCase;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramWebhookRequest request)
    {
        var result = await _handleMessageUseCase.Execute(request.UserId, request.Text);
        return Ok(new { result });
    }
}

public sealed class TelegramWebhookRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
