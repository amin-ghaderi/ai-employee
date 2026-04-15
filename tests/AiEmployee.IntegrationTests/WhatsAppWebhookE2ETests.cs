using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AiEmployee.IntegrationTests;

public sealed class WhatsAppWebhookVerifyFixture : PostgresWebApplicationFactory
{
    protected override void ConfigureWebHostExtras(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["WhatsApp:VerifyToken"] = "wa-verify-test-token",
                    });
            });
    }
}

/// <summary>Public WhatsApp webhook verification route (no admin key).</summary>
public sealed class WhatsAppWebhookE2ETests : IClassFixture<WhatsAppWebhookVerifyFixture>, IDisposable
{
    private readonly HttpClient _client;

    public WhatsAppWebhookE2ETests(WhatsAppWebhookVerifyFixture factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task WhatsApp_webhook_verify_without_admin_key_returns_challenge()
    {
        var url =
            "/api/whatsapp/webhook?hub.mode=subscribe&hub.verify_token=wa-verify-test-token&hub.challenge=ECHO123";
        var res = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("ECHO123", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task WhatsApp_webhook_verify_rejects_wrong_token()
    {
        var url = "/api/whatsapp/webhook?hub.mode=subscribe&hub.verify_token=wrong&hub.challenge=x";
        var res = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
