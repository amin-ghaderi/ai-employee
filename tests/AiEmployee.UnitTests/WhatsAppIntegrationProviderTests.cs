using System.Net;
using AiEmployee.Application.Integrations;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Integrations.WhatsApp;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AiEmployee.UnitTests;

public sealed class WhatsAppIntegrationProviderTests
{
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public HttpStatusCode ResponseCode { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(
                new HttpResponseMessage(ResponseCode) { Content = new StringContent("{\"success\":true}") });
        }
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public TestHttpClientFactory(HttpClient client) => _client = client;

        public HttpClient CreateClient(string name) => _client;
    }

    private static BotIntegration Integration(string externalId) =>
        new(Guid.NewGuid(), Guid.NewGuid(), IntegrationProviders.WhatsApp, externalId, true);

    private static WhatsAppIntegrationProvider CreateProvider(
        RecordingHandler handler,
        WhatsAppSettings settings)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://graph.facebook.com/") };
        var factory = new TestHttpClientFactory(client);
        return new WhatsAppIntegrationProvider(
            factory,
            Options.Create(settings),
            NullLogger<WhatsAppIntegrationProvider>.Instance);
    }

    [Fact]
    public async Task SyncWebhookAsync_posts_subscribed_apps_with_bearer_token()
    {
        var handler = new RecordingHandler();
        var settings = new WhatsAppSettings
        {
            AccessToken = "test-token",
            PhoneNumberId = "123456789",
            GraphApiVersion = "v19.0",
        };
        var sut = CreateProvider(handler, settings);
        var integration = Integration("https://example.com/webhook");

        var result = await sut.SyncWebhookAsync(integration, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("123456789/subscribed_apps", handler.LastRequest.RequestUri!.ToString(), StringComparison.Ordinal);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("test-token", handler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task SyncWebhookAsync_fails_when_callback_url_missing()
    {
        var handler = new RecordingHandler();
        var sut = CreateProvider(
            handler,
            new WhatsAppSettings { AccessToken = "t", PhoneNumberId = "p", GraphApiVersion = "v19.0" });
        var integration = new BotIntegration(Guid.NewGuid(), Guid.NewGuid(), IntegrationProviders.WhatsApp, " ", true);

        var result = await sut.SyncWebhookAsync(integration, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(IntegrationWebhookFailureCategory.BadRequestGuard, result.FailureCategory);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task SyncWebhookAsync_fails_when_callback_not_https()
    {
        var handler = new RecordingHandler();
        var sut = CreateProvider(
            handler,
            new WhatsAppSettings { AccessToken = "t", PhoneNumberId = "p", GraphApiVersion = "v19.0" });

        var result = await sut.SyncWebhookAsync(integration: Integration("http://insecure.example/hook"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(IntegrationWebhookFailureCategory.BadRequestGuard, result.FailureCategory);
    }

    [Fact]
    public async Task SyncWebhookAsync_fails_when_configuration_incomplete()
    {
        var handler = new RecordingHandler();
        var sut = CreateProvider(handler, new WhatsAppSettings { AccessToken = "", PhoneNumberId = "", GraphApiVersion = "v19.0" });

        var result = await sut.SyncWebhookAsync(Integration("https://example.com/hook"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(IntegrationWebhookFailureCategory.BadRequestGuard, result.FailureCategory);
        Assert.Contains("incomplete", result.Message ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SyncWebhookAsync_maps_meta_http_error_to_upstream()
    {
        var handler = new RecordingHandler { ResponseCode = HttpStatusCode.BadRequest };
        var sut = CreateProvider(
            handler,
            new WhatsAppSettings { AccessToken = "t", PhoneNumberId = "p", GraphApiVersion = "v19.0" });

        var result = await sut.SyncWebhookAsync(Integration("https://example.com/hook"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(IntegrationWebhookFailureCategory.UpstreamProviderError, result.FailureCategory);
        Assert.Equal(400, result.ProviderErrorCode);
    }

    [Fact]
    public async Task GetWebhookInfoAsync_returns_configured_url()
    {
        var handler = new RecordingHandler();
        var sut = CreateProvider(
            handler,
            new WhatsAppSettings { AccessToken = "t", PhoneNumberId = "p", GraphApiVersion = "v19.0" });

        var result = await sut.GetWebhookInfoAsync(Integration("https://hooks.example.com/wa"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("https://hooks.example.com/wa", result.Info?.Url);
    }

    [Fact]
    public async Task DeleteWebhookAsync_succeeds_when_graph_returns_ok()
    {
        var handler = new RecordingHandler();
        var sut = CreateProvider(
            handler,
            new WhatsAppSettings { AccessToken = "t", PhoneNumberId = "p", GraphApiVersion = "v19.0" });

        var result = await sut.DeleteWebhookAsync(Integration("https://example.com/h"), true, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
    }

    [Fact]
    public void ProviderId_is_whatsapp()
    {
        var handler = new RecordingHandler();
        var sut = CreateProvider(handler, new WhatsAppSettings());
        Assert.Equal(IntegrationProviders.WhatsApp, sut.ProviderId);
        Assert.True(sut.SupportsWebhookLifecycle);
    }

}
