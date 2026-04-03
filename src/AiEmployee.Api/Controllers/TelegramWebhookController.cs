using AiEmployee.Api.Models;
using AiEmployee.Application.UseCases;
using AiEmployee.Infrastructure.Telegram;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramWebhookController : ControllerBase
{
    private readonly JudgeUseCase _judgeUseCase;
    private readonly ITelegramClient _telegramClient;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        JudgeUseCase judgeUseCase,
        ITelegramClient telegramClient,
        ILogger<TelegramWebhookController> logger)
    {
        _judgeUseCase = judgeUseCase;
        _telegramClient = telegramClient;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdate? update)
    {
        try
        {
            if (update?.Message?.Chat is null || string.IsNullOrEmpty(update.Message.Text))
            {
                return Ok();
            }

            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text;
            var userId = chatId.ToString();

            var result = await _judgeUseCase.Execute(userId, text);

            var responseText =
                $"🏆 Winner: {result.Winner}\n" +
                $"💡 Reason: {result.Reason}";

            await _telegramClient.SendMessageAsync(chatId, responseText);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram webhook processing failed");
            return Ok();
        }
    }
}
