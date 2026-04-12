using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.UnitTests;

public sealed class IntegrationProvidersTests
{
    [Theory]
    [InlineData("telegram", "telegram")]
    [InlineData(" Telegram ", "telegram")]
    [InlineData("TELEGRAM", "telegram")]
    [InlineData("whatsapp", "whatsapp")]
    [InlineData("whatsapp-cloud", "whatsapp")]
    [InlineData("META-WHATSAPP", "whatsapp")]
    [InlineData("Web", "web")]
    [InlineData("generic-webhook", "generic-webhook")]
    [InlineData("WEBHOOK", "generic-webhook")]
    [InlineData("generic", "generic-webhook")]
    [InlineData("Custom", "generic-webhook")]
    [InlineData("slack", "slack")]
    [InlineData("SLACK-EVENTS", "slack")]
    [InlineData(" slack-api ", "slack")]
    public void TryResolveFromChannel_maps_known_channels(string channel, string expected)
    {
        Assert.Equal(expected, IntegrationProviders.TryResolveFromChannel(channel));
    }

    [Theory]
    [InlineData("telegram1")]
    [InlineData("ai chat assistant telegram")]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolveFromChannel_returns_null_for_unknown(string channel)
    {
        Assert.Null(IntegrationProviders.TryResolveFromChannel(channel));
    }

    [Fact]
    public void SupportsAdminWebhookLifecycle_true_for_telegram_generic_whatsapp_and_slack()
    {
        Assert.True(IntegrationProviders.SupportsAdminWebhookLifecycle(IntegrationProviders.Telegram));
        Assert.True(IntegrationProviders.SupportsAdminWebhookLifecycle(IntegrationProviders.GenericWebhook));
        Assert.True(IntegrationProviders.SupportsAdminWebhookLifecycle(IntegrationProviders.WhatsApp));
        Assert.True(IntegrationProviders.SupportsAdminWebhookLifecycle(IntegrationProviders.Slack));
        Assert.True(IntegrationProviders.SupportsAdminWebhookLifecycle("SLACK"));
        Assert.False(IntegrationProviders.SupportsAdminWebhookLifecycle(IntegrationProviders.Web));
        Assert.False(IntegrationProviders.SupportsAdminWebhookLifecycle(null));
    }
}
