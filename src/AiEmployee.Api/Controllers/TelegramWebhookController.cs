using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Messaging;
using AiEmployee.Infrastructure.Telegram;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

/// <summary>
/// Telegram Bot API webhook. This route is not protected: <see cref="Middleware.AdminAuthMiddleware"/> only applies to paths under <c>/admin</c>.
/// </summary>
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

    /// <summary>Legacy single-bot webhook. Resolves integration via <c>Telegram:BotToken</c>, a single enabled integration, or fails if ambiguous.</summary>
    [HttpPost("webhook")]
    public Task<IActionResult> Webhook([FromBody] TelegramUpdate? update, CancellationToken cancellationToken) =>
        HandleWebhookAsync(telegramIntegrationId: null, update, cancellationToken);

    /// <summary>Multi-bot webhook: <c>integrationId</c> is the <c>BotIntegrations.Id</c> row for this bot (set Telegram webhook URL accordingly).</summary>
    [HttpPost("webhook/{integrationId:guid}")]
    public Task<IActionResult> WebhookForIntegration(
        Guid integrationId,
        [FromBody] TelegramUpdate? update,
        CancellationToken cancellationToken) =>
        HandleWebhookAsync(integrationId, update, cancellationToken);

    private async Task<IActionResult> HandleWebhookAsync(
        Guid? telegramIntegrationId,
        TelegramUpdate? update,
        CancellationToken cancellationToken)
    {
        if (update is null)
        {
            _logger.LogWarning("Telegram webhook: deserialized body is null (empty or invalid JSON).");
            return Ok();
        }

        _logger.LogInformation(
            "Telegram webhook: received update_id={UpdateId}, has_message={HasMessage}, text_length={TextLength}, integration_route={IntegrationId}",
            update.UpdateId,
            update.Message is not null,
            update.Message?.Text?.Length ?? 0,
            telegramIntegrationId);

        IncomingMessage? incoming;
        try
        {
            incoming = await _channelAdapter
                .MapAsync(update, telegramIntegrationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Telegram webhook: integration resolution failed for update_id={UpdateId}", update.UpdateId);
            return Ok();
        }

        if (incoming is null)
        {
            _logger.LogInformation(
                "Telegram webhook: update_id={UpdateId} ignored (no mappable text message; e.g. callback_query, edited_message, or non-text).",
                update.UpdateId);
            return Ok();
        }

        try
        {
            var previewLen = Math.Min(incoming.Text?.Length ?? 0, 120);
            var textPreview = previewLen == 0
                ? string.Empty
                : incoming.Text!.Substring(0, previewLen).ReplaceLineEndings(" ");
            _logger.LogInformation(
                "Telegram webhook: dispatching to IncomingMessageHandler | update_id={UpdateId} channel={Channel} chat_id={ChatId} user_id={UserId} text_length={TextLength} text_preview={TextPreview}",
                update.UpdateId,
                incoming.Channel,
                incoming.ExternalChatId,
                incoming.ExternalUserId,
                incoming.Text?.Length ?? 0,
                textPreview);
            await _incomingMessageHandler.HandleAsync(incoming, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Telegram webhook: completed update_id={UpdateId}",
                update.UpdateId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Telegram webhook: processing failed for update_id={UpdateId}; returning 200 to avoid Telegram retries.",
                update.UpdateId);
            return Ok();
        }
    }
}
