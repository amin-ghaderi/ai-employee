namespace AiEmployee.Application.Integrations;

/// <summary>Maps provider-specific webhook DTOs to the neutral integration webhook contract.</summary>
public static class IntegrationWebhookMapper
{
    public static IntegrationWebhookSyncResult FromTelegram(TelegramWebhookSyncResult tg)
    {
        if (tg.Success)
            return IntegrationWebhookSyncResult.Ok(tg.ConfiguredWebhookUrl ?? string.Empty, tg.TelegramDescription);

        var category = ClassifyUpstreamOrClient(tg.Message);
        return IntegrationWebhookSyncResult.Failed(
            category,
            tg.Message ?? tg.TelegramDescription ?? "Webhook sync failed.",
            tg.TelegramErrorCode,
            tg.TelegramDescription,
            tg.ConfiguredWebhookUrl);
    }

    public static IntegrationWebhookInfoResult FromTelegram(TelegramWebhookInfoResult tg)
    {
        if (tg.Success)
        {
            var info = tg.Info is null
                ? null
                : new IntegrationWebhookInfoData(
                    tg.Info.Url,
                    tg.Info.PendingUpdateCount,
                    tg.Info.LastErrorMessage,
                    tg.Info.LastErrorDate,
                    tg.Info.HasCustomCertificate,
                    tg.Info.MaxConnections);

            return new IntegrationWebhookInfoResult(true, IntegrationWebhookFailureCategory.None, null, null, tg.TelegramDescription, info);
        }

        var category = ClassifyUpstreamOrClient(tg.Message);
        return new IntegrationWebhookInfoResult(
            false,
            category,
            tg.Message ?? tg.TelegramDescription ?? "Webhook status failed.",
            tg.TelegramErrorCode,
            tg.TelegramDescription,
            null);
    }

    public static IntegrationWebhookDeleteResult FromTelegram(TelegramWebhookDeleteResult tg)
    {
        if (tg.Success)
            return new IntegrationWebhookDeleteResult(true, IntegrationWebhookFailureCategory.None, null, null, tg.TelegramDescription);

        var category = ClassifyUpstreamOrClient(tg.Message);
        return new IntegrationWebhookDeleteResult(
            false,
            category,
            tg.Message ?? tg.TelegramDescription ?? "Webhook delete failed.",
            tg.TelegramErrorCode,
            tg.TelegramDescription);
    }

    private static IntegrationWebhookFailureCategory ClassifyUpstreamOrClient(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return IntegrationWebhookFailureCategory.UpstreamProviderError;

        if (message.Contains("PublicBaseUrl", StringComparison.OrdinalIgnoreCase)
            || message.Contains("must use HTTPS in Production", StringComparison.OrdinalIgnoreCase)
            || message.Contains("absolute URI", StringComparison.OrdinalIgnoreCase))
            return IntegrationWebhookFailureCategory.ClientConfiguration;

        return IntegrationWebhookFailureCategory.UpstreamProviderError;
    }
}
