using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Messaging;
using AiEmployee.Infrastructure.Telegram;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramWebhookController : ControllerBase
{
    private readonly IChannelAdapter _channelAdapter;
    private readonly IIncomingMessageHandler _incomingMessageHandler;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        IChannelAdapter channelAdapter,
        IIncomingMessageHandler incomingMessageHandler,
        ILogger<TelegramWebhookController> logger)
    {
        _channelAdapter = channelAdapter;
        _incomingMessageHandler = incomingMessageHandler;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdate? update)
    {
        var incoming = _channelAdapter.Map(update);
        if (incoming is null)
            return Ok();

        try
        {
            await _incomingMessageHandler.HandleAsync(incoming);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram webhook processing failed");
            return StatusCode(500);
        }
    }
}
