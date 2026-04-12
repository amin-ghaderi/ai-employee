namespace AiEmployee.Domain.BotConfiguration;

/// <summary>Canonical <see cref="BotIntegration.Channel"/> values stored in the database.</summary>
public static class BotIntegrationChannelNames
{
    public const string Telegram = "telegram";
    public const string WhatsApp = "whatsapp";
    public const string Web = "web";

    /// <summary>Trim + lower-invariant for persisted channel keys (matches admin create/update normalization).</summary>
    public static string NormalizeChannelValue(string channel) =>
        string.IsNullOrWhiteSpace(channel) ? string.Empty : channel.Trim().ToLowerInvariant();

    /// <summary>Whether <paramref name="channel"/> refers to Telegram (ignores surrounding whitespace and casing).</summary>
    public static bool IsTelegramChannel(string? channel) =>
        !string.IsNullOrWhiteSpace(channel) &&
        string.Equals(channel.Trim(), Telegram, StringComparison.OrdinalIgnoreCase);
}
