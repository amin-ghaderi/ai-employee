using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AiEmployee.IntegrationTests;

public sealed class SlackWebhookFixture : WebApplicationFactory<Program>
{
    public const string TestSigningSecret = "test-slack-signing-secret-for-e2e";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Slack:SigningSecret"] = TestSigningSecret,
                    });
            });
    }
}

public sealed class SlackWebhookE2ETests : IClassFixture<SlackWebhookFixture>, IDisposable
{
    private readonly HttpClient _client;

    public SlackWebhookE2ETests(SlackWebhookFixture factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public void Dispose() => _client.Dispose();

    private static string ComputeSlackSignature(string signingSecret, string timestamp, string body)
    {
        var baseString = $"v0:{timestamp}:{body}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
        return "v0=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public async Task Slack_url_verification_returns_challenge_json()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        const string body = """{"type":"url_verification","challenge":"slack-challenge-xyz"}""";
        var sig = ComputeSlackSignature(SlackWebhookFixture.TestSigningSecret, ts, body);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/slack/events")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        req.Headers.TryAddWithoutValidation("X-Slack-Request-Timestamp", ts);
        req.Headers.TryAddWithoutValidation("X-Slack-Signature", sig);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);

        var json = await res.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal("slack-challenge-xyz", doc.RootElement.GetProperty("challenge").GetString());
    }

    [Fact]
    public async Task Slack_signed_event_callback_without_integration_resolution_returns_bad_request()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        const string body = """{"type":"event_callback","event":{"type":"message"}}""";
        var sig = ComputeSlackSignature(SlackWebhookFixture.TestSigningSecret, ts, body);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/slack/events")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        req.Headers.TryAddWithoutValidation("X-Slack-Request-Timestamp", ts);
        req.Headers.TryAddWithoutValidation("X-Slack-Signature", sig);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Slack_signed_event_callback_unknown_integration_id_returns_not_found()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        const string body = """{"type":"event_callback","event":{"type":"message","user":"U1","text":"x","channel":"C1"}}""";
        var sig = ComputeSlackSignature(SlackWebhookFixture.TestSigningSecret, ts, body);
        var id = Guid.NewGuid();

        using var req = new HttpRequestMessage(HttpMethod.Post, $"/api/slack/events/{id}")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        req.Headers.TryAddWithoutValidation("X-Slack-Request-Timestamp", ts);
        req.Headers.TryAddWithoutValidation("X-Slack-Signature", sig);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Slack_rejects_invalid_signature()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        const string body = """{"type":"event_callback"}""";

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/slack/events")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        req.Headers.TryAddWithoutValidation("X-Slack-Request-Timestamp", ts);
        req.Headers.TryAddWithoutValidation("X-Slack-Signature", "v0=deadbeef");

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
