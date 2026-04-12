namespace AiEmployee.Application.Telegram;

/// <summary>Masks bot tokens for logs (never log the secret segment after <c>:</c>).</summary>
public static class TelegramTokenMasking
{
    /// <summary>Returns a masked representation like <c>123456789:***</c> or <c>(empty)</c>.</summary>
    public static string MaskBotToken(string? token)
    {
        var t = token?.Trim();
        if (string.IsNullOrEmpty(t))
            return "(empty)";

        var colon = t.IndexOf(':');
        if (colon <= 0 || colon >= t.Length - 1)
            return "(invalid format: expected bot_id:secret)";

        return $"{t[..colon]}:***";
    }
}
