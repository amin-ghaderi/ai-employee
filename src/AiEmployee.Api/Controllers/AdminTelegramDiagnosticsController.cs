using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Telegram;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AiEmployee.Api.Controllers;

/// <summary>Admin-only diagnostics for Telegram token, API reachability, webhook, and DB integration alignment.</summary>
[ApiController]
[Route("admin/telegram")]
public sealed class AdminTelegramDiagnosticsController : ControllerBase
{
    private readonly ITelegramClient _telegramClient;
    private readonly IBotIntegrationRepository _integrations;
    private readonly IOptions<TelegramSettings> _telegramSettings;

    public AdminTelegramDiagnosticsController(
        ITelegramClient telegramClient,
        IBotIntegrationRepository integrations,
        IOptions<TelegramSettings> telegramSettings)
    {
        _telegramClient = telegramClient;
        _integrations = integrations;
        _telegramSettings = telegramSettings;
    }

    [HttpGet("diagnostics")]
    public async Task<ActionResult<TelegramDiagnosticsReportDto>> GetDiagnostics(CancellationToken cancellationToken)
    {
        var configured = _telegramSettings.Value.BotToken?.Trim() ?? string.Empty;
        var maskedConfigured = TelegramTokenUtilities.MaskBotToken(configured);

        var http = await _telegramClient.FetchDiagnosticsAsync(cancellationToken).ConfigureAwait(false);

        var all = await _integrations.ListAsync(botId: null, cancellationToken).ConfigureAwait(false);
        var telegramRows = all
            .Where(i => BotIntegrationChannelNames.IsTelegramChannel(i.Channel))
            .Select(i => new TelegramIntegrationDiagnosticRowDto(
                i.Id,
                i.BotId,
                TelegramTokenUtilities.MaskBotToken(i.ExternalId),
                i.IsEnabled,
                !string.IsNullOrEmpty(configured) &&
                string.Equals(i.ExternalId.Trim(), configured, StringComparison.Ordinal)))
            .ToList();

        var matchesEnabled = telegramRows.Any(r => r.MatchesConfiguredToken && r.IsEnabled);

        return Ok(new TelegramDiagnosticsReportDto(
            maskedConfigured,
            string.IsNullOrEmpty(configured) ? "missing" : "present",
            http,
            telegramRows,
            matchesEnabled));
    }
}

public sealed record TelegramIntegrationDiagnosticRowDto(
    Guid Id,
    Guid BotId,
    string MaskedExternalId,
    bool IsEnabled,
    bool MatchesConfiguredToken);

public sealed record TelegramDiagnosticsReportDto(
    string MaskedConfiguredToken,
    string ConfiguredTokenPresence,
    TelegramHttpDiagnostics TelegramApi,
    IReadOnlyList<TelegramIntegrationDiagnosticRowDto> TelegramIntegrations,
    bool ConfiguredTokenMatchesAnyEnabledIntegration);
