using AiEmployee.Application.Integrations;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Integrations.Slack;
using Microsoft.Extensions.Logging.Abstractions;

namespace AiEmployee.UnitTests;

public sealed class SlackIntegrationProviderTests
{
    private static BotIntegration Integration(string channel, string externalId) =>
        new(Guid.NewGuid(), Guid.NewGuid(), channel, externalId, true);

    [Fact]
    public async Task SyncWebhookAsync_succeeds_for_valid_https_request_url()
    {
        var sut = new SlackIntegrationProvider(NullLogger<SlackIntegrationProvider>.Instance);
        var integration = Integration("slack", "https://example.com/api/slack/events");

        var result = await sut.SyncWebhookAsync(integration, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("https://example.com/api/slack/events", result.ConfiguredWebhookUrl);
    }

    [Fact]
    public async Task SyncWebhookAsync_fails_when_request_url_missing()
    {
        var sut = new SlackIntegrationProvider(NullLogger<SlackIntegrationProvider>.Instance);
        var integration = Integration("slack", " ");

        var result = await sut.SyncWebhookAsync(integration, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(IntegrationWebhookFailureCategory.BadRequestGuard, result.FailureCategory);
        Assert.Contains("missing", result.Message ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SyncWebhookAsync_fails_when_url_not_https()
    {
        var sut = new SlackIntegrationProvider(NullLogger<SlackIntegrationProvider>.Instance);
        var integration = Integration("slack", "http://example.com/hook");

        var result = await sut.SyncWebhookAsync(integration, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(IntegrationWebhookFailureCategory.BadRequestGuard, result.FailureCategory);
    }

    [Fact]
    public void ProviderId_is_slack_and_lifecycle_supported()
    {
        var sut = new SlackIntegrationProvider(NullLogger<SlackIntegrationProvider>.Instance);
        Assert.Equal(IntegrationProviders.Slack, sut.ProviderId);
        Assert.True(sut.SupportsWebhookLifecycle);
    }

    [Fact]
    public async Task GetWebhookInfoAsync_returns_url()
    {
        var sut = new SlackIntegrationProvider(NullLogger<SlackIntegrationProvider>.Instance);
        var result = await sut.GetWebhookInfoAsync(
            Integration("slack-events", "https://hooks.example/slack"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("https://hooks.example/slack", result.Info?.Url);
    }

    [Fact]
    public async Task DeleteWebhookAsync_succeeds_without_remote_call()
    {
        var sut = new SlackIntegrationProvider(NullLogger<SlackIntegrationProvider>.Instance);
        var result = await sut.DeleteWebhookAsync(
            Integration("slack-api", "https://example.com/e"),
            dropPendingUpdates: true,
            CancellationToken.None);

        Assert.True(result.Success);
    }
}
