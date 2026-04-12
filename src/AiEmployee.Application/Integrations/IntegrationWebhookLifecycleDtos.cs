namespace AiEmployee.Application.Integrations;

/// <summary>Classifies failures from provider-agnostic webhook admin operations for HTTP mapping.</summary>
public enum IntegrationWebhookFailureCategory
{
    None = 0,
    IntegrationNotFound = 1,
    BadRequestGuard = 2,
    ClientConfiguration = 3,
    UpstreamProviderError = 4,
    UnsupportedProvider = 5,
    InternalError = 6,
}

/// <summary>Outcome of registering a webhook for an integration (provider-agnostic envelope).</summary>
public sealed record IntegrationWebhookSyncResult(
    bool Success,
    IntegrationWebhookFailureCategory FailureCategory,
    string? Message,
    int? ProviderErrorCode,
    string? ProviderDescription,
    string? ConfiguredWebhookUrl)
{
    public static IntegrationWebhookSyncResult Ok(string configuredWebhookUrl, string? providerDescription) =>
        new(true, IntegrationWebhookFailureCategory.None, null, null, providerDescription, configuredWebhookUrl);

    public static IntegrationWebhookSyncResult Failed(
        IntegrationWebhookFailureCategory category,
        string message,
        int? providerErrorCode = null,
        string? providerDescription = null,
        string? configuredWebhookUrl = null) =>
        new(false, category, message, providerErrorCode, providerDescription, configuredWebhookUrl);
}

/// <summary>Webhook endpoint metadata returned by a provider (normalized shape).</summary>
public sealed record IntegrationWebhookInfoData(
    string? Url,
    int? PendingUpdateCount,
    string? LastErrorMessage,
    long? LastErrorDate,
    bool? HasCustomCertificate,
    int? MaxConnections);

/// <summary>Outcome of reading webhook status for an integration.</summary>
public sealed record IntegrationWebhookInfoResult(
    bool Success,
    IntegrationWebhookFailureCategory FailureCategory,
    string? Message,
    int? ProviderErrorCode,
    string? ProviderDescription,
    IntegrationWebhookInfoData? Info);

/// <summary>Outcome of deleting a webhook for an integration.</summary>
public sealed record IntegrationWebhookDeleteResult(
    bool Success,
    IntegrationWebhookFailureCategory FailureCategory,
    string? Message,
    int? ProviderErrorCode,
    string? ProviderDescription);
