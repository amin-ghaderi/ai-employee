namespace AiEmployee.Application.Dtos.Integrations;

/// <summary>Admin API shape for Telegram webhook operations (camelCase in JSON).</summary>
public sealed record AdminTelegramWebhookSummaryDto(
    string? WebhookUrl,
    string Status,
    string? LastError,
    DateTime? LastSyncedAt);
