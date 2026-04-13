using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Messaging;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Telegram;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.Messaging;

public sealed class TelegramChannelAdapter : IChannelAdapter
{
    private readonly IBotIntegrationRepository _integrations;
    private readonly IOptions<TelegramSettings> _telegramSettings;
    private readonly ILogger<TelegramChannelAdapter> _logger;

    public TelegramChannelAdapter(
        IBotIntegrationRepository integrations,
        IOptions<TelegramSettings> telegramSettings,
        ILogger<TelegramChannelAdapter> logger)
    {
        _integrations = integrations;
        _telegramSettings = telegramSettings;
        _logger = logger;
    }

    public async Task<IncomingMessage?> MapAsync(
        object? rawRequest,
        Guid? telegramIntegrationId = null,
        CancellationToken cancellationToken = default)
    {
        if (rawRequest is not TelegramUpdate update)
            return null;

        if (update.Message?.Chat is null || string.IsNullOrWhiteSpace(update.Message.Text))
            return null;

        var externalChatId = update.Message.Chat.Id.ToString();
        var externalUserId = update.Message.From?.Id.ToString() ?? externalChatId;
        var from = update.Message.From;

        string integrationExternalId;
        try
        {
            integrationExternalId = await ResolveIntegrationExternalIdAsync(
                telegramIntegrationId,
                cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "TelegramChannelAdapter: cannot resolve integration external id for webhook.");
            throw;
        }

        var metadata = new Dictionary<string, string>
        {
            [IncomingMessageMetadataKeys.IntegrationExternalId] = integrationExternalId,
        };
        if (!string.IsNullOrEmpty(from?.Username))
            metadata[IncomingMessageMetadataKeys.Username] = from.Username;
        if (!string.IsNullOrEmpty(from?.FirstName))
            metadata[IncomingMessageMetadataKeys.FirstName] = from.FirstName;
        if (!string.IsNullOrEmpty(from?.LastName))
            metadata[IncomingMessageMetadataKeys.LastName] = from.LastName;

        metadata[IncomingMessageMetadataKeys.TelegramBotScopeKey] =
            BuildTelegramBotScopeKey(telegramIntegrationId, integrationExternalId);
        metadata[IncomingMessageMetadataKeys.TelegramUpdateId] =
            update.UpdateId.ToString(CultureInfo.InvariantCulture);

        return new IncomingMessage(
            BotIntegrationChannelNames.Telegram,
            externalUserId,
            externalChatId,
            update.Message.Text.Trim(),
            metadata);
    }

    private async Task<string> ResolveIntegrationExternalIdAsync(
        Guid? telegramIntegrationId,
        CancellationToken cancellationToken)
    {
        if (telegramIntegrationId is Guid id)
        {
            var row = await _integrations.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (row is null)
                throw new InvalidOperationException($"No BotIntegration found for id '{id}'.");

            if (!row.IsEnabled)
                throw new InvalidOperationException($"BotIntegration '{id}' is disabled.");

            if (!BotIntegrationChannelNames.IsTelegramChannel(row.Channel))
                throw new InvalidOperationException($"BotIntegration '{id}' is not a Telegram integration.");

            if (string.IsNullOrWhiteSpace(row.ExternalId))
                throw new InvalidOperationException($"BotIntegration '{id}' has an empty ExternalId (token).");

            return row.ExternalId.Trim();
        }

        var configured = _telegramSettings.Value.BotToken?.Trim();
        if (!string.IsNullOrEmpty(configured))
        {
            _logger.LogDebug("Telegram webhook: using Telegram:BotToken from configuration for integrationExternalId (legacy path).");
            return configured;
        }

        var all = await _integrations.ListAsync(botId: null, cancellationToken).ConfigureAwait(false);
        var enabledTelegram = all
            .Where(i =>
                BotIntegrationChannelNames.IsTelegramChannel(i.Channel) &&
                i.IsEnabled &&
                !string.IsNullOrWhiteSpace(i.ExternalId))
            .OrderBy(i => i.Id)
            .ToList();

        if (enabledTelegram.Count == 0)
        {
            throw new InvalidOperationException(
                "No enabled Telegram BotIntegration exists and Telegram:BotToken is not set. " +
                "Add an integration or set a webhook URL that includes the integration id: POST /api/telegram/webhook/{integrationId}.");
        }

        if (enabledTelegram.Count > 1)
        {
            throw new InvalidOperationException(
                "Multiple enabled Telegram integrations exist; Telegram:BotToken is not set. " +
                "Register each bot's webhook as POST /api/telegram/webhook/{integrationId} with that row's Id, " +
                "or set Telegram:BotToken to disambiguate.");
        }

        var token = enabledTelegram[0].ExternalId.Trim();
        _logger.LogInformation(
            "Telegram webhook: using sole enabled integration {IntegrationId} (masked token {Masked}).",
            enabledTelegram[0].Id,
            TelegramTokenUtilities.MaskBotToken(token));
        return token;
    }

    /// <summary>
    /// Stable per-bot key: integration route id when present, otherwise short hash of the bot token string.
    /// </summary>
    private static string BuildTelegramBotScopeKey(Guid? telegramIntegrationId, string integrationExternalId)
    {
        if (telegramIntegrationId is { } id)
            return $"i:{id:N}";

        var trimmed = integrationExternalId.Trim();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(trimmed));
        return "t:" + Convert.ToHexString(hash.AsSpan(0, 16));
    }
}
