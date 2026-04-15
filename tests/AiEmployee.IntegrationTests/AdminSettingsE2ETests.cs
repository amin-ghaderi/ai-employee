using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AiEmployee.IntegrationTests;

public sealed class AdminSettingsE2ETests : IClassFixture<PostgresWebApplicationFactory>, IDisposable
{
    private const string ValidAdminKey = "your-secret-key";

    private readonly HttpClient _client;

    public AdminSettingsE2ETests(PostgresWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Get_public_base_url_without_key_returns_401()
    {
        var res = await _client.GetAsync("/admin/settings/public-base-url");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Get_public_base_url_with_valid_key_returns_200()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/admin/settings/public-base-url");
        req.Headers.Add("X-Admin-Key", ValidAdminKey);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<PublicBaseUrlResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(body);
    }

    [Fact]
    public async Task Put_public_base_url_invalid_uri_returns_400()
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, "/admin/settings/public-base-url");
        req.Headers.Add("X-Admin-Key", ValidAdminKey);
        req.Content = JsonContent.Create(new { publicBaseUrl = "not-a-uri" });
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    private sealed class PublicBaseUrlResponse
    {
        public string? PublicBaseUrl { get; set; }
    }
}
