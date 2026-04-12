namespace AiEmployee.Domain.BotConfiguration;

/// <summary>
/// Canonical provider keys for <see cref="BotIntegration"/>. Phase 1: derived from persisted <c>Channel</c> until a dedicated column exists.
/// </summary>
public static class IntegrationProviders
{
    public const string Telegram = BotIntegrationChannelNames.Telegram;
    public const string WhatsApp = BotIntegrationChannelNames.WhatsApp;
    public const string Web = BotIntegrationChannelNames.Web;

    /// <summary>Custom outbound webhook URL stored in <see cref="BotIntegration.ExternalId"/> (admin &quot;sync&quot; validates URL only).</summary>
    public const string GenericWebhook = "generic-webhook";

    /// <summary>Slack Events API; <see cref="BotIntegration.ExternalId"/> holds the public Request URL (HTTPS).</summary>
    public const string Slack = "slack";

    /// <summary>
    /// Resolves a stable provider key from the persisted channel string (trim + case normalization via <see cref="BotIntegrationChannelNames.NormalizeChannelValue"/> rules).
    /// Returns <c>null</c> when the channel does not match a known provider (e.g. custom labels).
    /// </summary>
    public static string? TryResolveFromChannel(string? channel)
    {
        var normalized = BotIntegrationChannelNames.NormalizeChannelValue(channel ?? string.Empty);
        if (string.IsNullOrEmpty(normalized))
            return null;

        if (BotIntegrationChannelNames.IsTelegramChannel(normalized))
            return Telegram;

        if (IsWhatsAppChannel(normalized))
            return WhatsApp;

        if (string.Equals(normalized, Web, StringComparison.OrdinalIgnoreCase))
            return Web;

        if (IsSlackChannel(normalized))
            return Slack;

        if (IsGenericWebhookChannel(normalized))
            return GenericWebhook;

        return null;
    }

    /// <summary>Aliases that map to <see cref="Slack"/>.</summary>
    private static bool IsSlackChannel(string normalized) =>
        normalized is "slack" or "slack-events" or "slack-api";

    /// <summary>Aliases that map to <see cref="GenericWebhook"/>.</summary>
    private static bool IsGenericWebhookChannel(string normalized) =>
        normalized is "generic-webhook" or "webhook" or "generic" or "custom";

    /// <summary>Aliases that map to <see cref="WhatsApp"/> (Meta WhatsApp Cloud API).</summary>
    private static bool IsWhatsAppChannel(string normalized) =>
        normalized is "whatsapp" or "whatsapp-cloud" or "meta-whatsapp";

    /// <summary>
    /// Whether admin API can run webhook lifecycle (register / status / delete) for this provider.
    /// </summary>
    public static bool SupportsAdminWebhookLifecycle(string? providerKey) =>
        string.Equals(providerKey, Telegram, StringComparison.OrdinalIgnoreCase)
        || string.Equals(providerKey, GenericWebhook, StringComparison.OrdinalIgnoreCase)
        || string.Equals(providerKey, WhatsApp, StringComparison.OrdinalIgnoreCase)
        || string.Equals(providerKey, Slack, StringComparison.OrdinalIgnoreCase);
}
