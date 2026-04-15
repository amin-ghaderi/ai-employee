using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AiEmployee.IntegrationTests;

/// <summary>Validates Admin X-Admin-Key gating and stable JSON for webhook admin routes (no live Telegram calls for unknown integration id).</summary>
public sealed class AdminTelegramWebhookE2ETests : IClassFixture<PostgresWebApplicationFactory>, IDisposable
{
    private const string ValidAdminKey = "your-secret-key";

    private readonly HttpClient _client;

    public AdminTelegramWebhookE2ETests(PostgresWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Admin_webhook_status_without_header_returns_401()
    {
        var id = Guid.NewGuid();
        var res = await _client.GetAsync($"/admin/integrations/{id}/webhook-status");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Admin_webhook_status_with_wrong_key_returns_401()
    {
        var id = Guid.NewGuid();
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/admin/integrations/{id}/webhook-status");
        req.Headers.Add("X-Admin-Key", "wrong-key");
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Admin_webhook_status_with_valid_key_unknown_integration_returns_404_not_found_dto()
    {
        var id = Guid.NewGuid();
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/admin/integrations/{id}/webhook-status");
        req.Headers.Add("X-Admin-Key", ValidAdminKey);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<AdminWebhookSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(body);
        Assert.Equal("not_found", body.Status);
        Assert.False(string.IsNullOrEmpty(body.LastError));
    }

    [Fact]
    public async Task Admin_integrations_list_without_key_returns_401()
    {
        var res = await _client.GetAsync("/admin/integrations");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Telegram_webhook_legacy_route_does_not_require_admin_key()
    {
        using var content = new StringContent("{}", new MediaTypeHeaderValue("application/json"));
        var res = await _client.PostAsync("/api/telegram/webhook", content);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Telegram_webhook_per_integration_route_does_not_require_admin_key()
    {
        var integrationId = Guid.NewGuid();
        using var content = new StringContent("{}", new MediaTypeHeaderValue("application/json"));
        var res = await _client.PostAsync($"/api/telegram/webhook/{integrationId}", content);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    private sealed class AdminWebhookSummaryResponse
    {
        public string? WebhookUrl { get; set; }
        public string? Status { get; set; }
        public string? LastError { get; set; }
        public string? LastSyncedAt { get; set; }
    }
}
