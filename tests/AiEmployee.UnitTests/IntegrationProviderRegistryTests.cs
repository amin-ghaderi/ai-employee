using AiEmployee.Application.Integrations;
using AiEmployee.Application.Integrations.Providers;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.UnitTests;

public sealed class IntegrationProviderRegistryTests
{
    private sealed class FakeTelegramProvider : IIntegrationProvider
    {
        public string ProviderId => IntegrationProviders.Telegram;

        public bool SupportsWebhookLifecycle => true;

        public Task<IntegrationWebhookSyncResult> SyncWebhookAsync(
            BotIntegration integration,
            CancellationToken cancellationToken) =>
            Task.FromResult(IntegrationWebhookSyncResult.Ok("https://example.com/hook", null));

        public Task<IntegrationWebhookInfoResult> GetWebhookInfoAsync(
            BotIntegration integration,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                new IntegrationWebhookInfoResult(true, IntegrationWebhookFailureCategory.None, null, null, null, null));

        public Task<IntegrationWebhookDeleteResult> DeleteWebhookAsync(
            BotIntegration integration,
            bool dropPendingUpdates,
            CancellationToken cancellationToken) =>
            Task.FromResult(new IntegrationWebhookDeleteResult(true, IntegrationWebhookFailureCategory.None, null, null, null));
    }

    private sealed class FakeWhatsAppProvider : IIntegrationProvider
    {
        public string ProviderId => IntegrationProviders.WhatsApp;

        public bool SupportsWebhookLifecycle => false;

        public Task<IntegrationWebhookSyncResult> SyncWebhookAsync(
            BotIntegration integration,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("not used");

        public Task<IntegrationWebhookInfoResult> GetWebhookInfoAsync(
            BotIntegration integration,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("not used");

        public Task<IntegrationWebhookDeleteResult> DeleteWebhookAsync(
            BotIntegration integration,
            bool dropPendingUpdates,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("not used");
    }

    [Fact]
    public void Resolve_returns_telegram_provider_case_insensitively()
    {
        var sut = new IntegrationProviderRegistry(new IIntegrationProvider[]
        {
            new FakeTelegramProvider(),
            new FakeWhatsAppProvider(),
        });

        Assert.Same(sut.Resolve("telegram"), sut.Resolve("Telegram"));
        Assert.Same(sut.Resolve("TELEGRAM"), sut.Resolve("telegram"));
    }

    [Fact]
    public void Resolve_returns_whatsapp_provider()
    {
        var wa = new FakeWhatsAppProvider();
        var sut = new IntegrationProviderRegistry(new IIntegrationProvider[] { new FakeTelegramProvider(), wa });

        Assert.Same(wa, sut.Resolve("whatsapp"));
    }

    [Fact]
    public void Resolve_returns_null_for_unknown_provider()
    {
        var sut = new IntegrationProviderRegistry(new IIntegrationProvider[] { new FakeTelegramProvider() });

        Assert.Null(sut.Resolve("slack"));
        Assert.Null(sut.Resolve(null));
        Assert.Null(sut.Resolve(""));
        Assert.Null(sut.Resolve("   "));
    }
}
