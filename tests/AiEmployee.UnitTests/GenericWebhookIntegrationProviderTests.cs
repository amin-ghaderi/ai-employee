using AiEmployee.Application.Integrations;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Integrations.GenericWebhook;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;

namespace AiEmployee.UnitTests;

public sealed class GenericWebhookIntegrationProviderTests
{
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class NullFileProvider : IFileProvider
    {
        public IDirectoryContents GetDirectoryContents(string subpath) =>
            NotFoundDirectoryContents.Singleton;

        public IFileInfo GetFileInfo(string subpath) => new NotFoundFileInfo(subpath);

        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }

    private static BotIntegration Integration(string channel, string externalId) =>
        new(Guid.NewGuid(), Guid.NewGuid(), channel, externalId, true);

    [Fact]
    public async Task SyncWebhookAsync_valid_https_url_succeeds()
    {
        var sut = new GenericWebhookIntegrationProvider(new FakeHostEnvironment());
        var integration = Integration(IntegrationProviders.GenericWebhook, "https://example.com/webhook/callback");

        var result = await sut.SyncWebhookAsync(integration, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("https://example.com/webhook/callback", result.ConfiguredWebhookUrl);
    }

    [Fact]
    public async Task SyncWebhookAsync_invalid_relative_url_fails()
    {
        var sut = new GenericWebhookIntegrationProvider(new FakeHostEnvironment());
        var integration = Integration(IntegrationProviders.GenericWebhook, "/relative/path");

        var result = await sut.SyncWebhookAsync(integration, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(IntegrationWebhookFailureCategory.BadRequestGuard, result.FailureCategory);
        Assert.Contains("absolute", result.Message ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SyncWebhookAsync_http_url_in_production_fails()
    {
        var sut = new GenericWebhookIntegrationProvider(new FakeHostEnvironment { EnvironmentName = Environments.Production });
        var integration = Integration(IntegrationProviders.GenericWebhook, "http://example.com/hook");

        var result = await sut.SyncWebhookAsync(integration, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(IntegrationWebhookFailureCategory.BadRequestGuard, result.FailureCategory);
        Assert.Contains("HTTPS", result.Message ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SyncWebhookAsync_http_url_allowed_in_development()
    {
        var sut = new GenericWebhookIntegrationProvider(new FakeHostEnvironment());
        var integration = Integration(IntegrationProviders.GenericWebhook, "http://localhost:9999/hook");

        var result = await sut.SyncWebhookAsync(integration, CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task SyncWebhookAsync_whitespace_only_fails()
    {
        var sut = new GenericWebhookIntegrationProvider(new FakeHostEnvironment());
        var integration = Integration(IntegrationProviders.GenericWebhook, "   ");

        var result = await sut.SyncWebhookAsync(integration, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("missing", result.Message ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetWebhookInfoAsync_returns_configured_url()
    {
        var sut = new GenericWebhookIntegrationProvider(new FakeHostEnvironment());
        var integration = Integration(IntegrationProviders.GenericWebhook, "https://hooks.example.com/x");

        var result = await sut.GetWebhookInfoAsync(integration, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Info);
        Assert.Equal("https://hooks.example.com/x", result.Info!.Url);
        Assert.Equal(0, result.Info.PendingUpdateCount);
        Assert.False(result.Info.HasCustomCertificate);
    }

    [Fact]
    public async Task GetWebhookInfoAsync_invalid_url_fails()
    {
        var sut = new GenericWebhookIntegrationProvider(new FakeHostEnvironment());
        var integration = Integration(IntegrationProviders.GenericWebhook, "not-a-uri");

        var result = await sut.GetWebhookInfoAsync(integration, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(IntegrationWebhookFailureCategory.BadRequestGuard, result.FailureCategory);
    }

    [Fact]
    public async Task DeleteWebhookAsync_succeeds_without_remote_call()
    {
        var sut = new GenericWebhookIntegrationProvider(new FakeHostEnvironment());
        var integration = Integration(IntegrationProviders.GenericWebhook, "https://example.com/h");

        var result = await sut.DeleteWebhookAsync(integration, dropPendingUpdates: true, CancellationToken.None);

        Assert.True(result.Success);
    }
}
