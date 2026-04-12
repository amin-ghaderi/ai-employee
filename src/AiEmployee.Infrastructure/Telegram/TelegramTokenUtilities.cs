using AiEmployee.Application.Telegram;

namespace AiEmployee.Infrastructure.Telegram;

/// <summary>Safe token descriptions for logs and admin diagnostics (never log the secret segment).</summary>
public static class TelegramTokenUtilities
{
    /// <summary>Returns a masked representation like <c>123456789:***</c> or <c>(empty)</c>.</summary>
    public static string MaskBotToken(string? token) => TelegramTokenMasking.MaskBotToken(token);

    /// <summary>Short summary suitable for startup logs.</summary>
    public static string DescribeForLog(string? token)
    {
        var t = token?.Trim();
        if (string.IsNullOrEmpty(t))
            return "missing";

        var colon = t.IndexOf(':');
        if (colon <= 0 || colon >= t.Length - 1)
            return "present but invalid format";

        return $"configured, masked={MaskBotToken(t)}";
    }
}
