using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AiEmployee.IntegrationTests;

/// <summary>Ensures Persona API surface includes <c>promptExtensions</c> after Phase 2 (contract stability).</summary>
public sealed class AdminPersonasPromptExtensionsTests : IClassFixture<PostgresWebApplicationFactory>, IDisposable
{
    private const string ValidAdminKey = "your-secret-key";

    private readonly HttpClient _client;

    public AdminPersonasPromptExtensionsTests(PostgresWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Admin_personas_list_returns_prompt_extensions_object_per_persona()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/admin/personas");
        req.Headers.Add("X-Admin-Key", ValidAdminKey);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        await using var stream = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() > 0);

        var first = doc.RootElement[0];
        Assert.True(
            first.TryGetProperty("promptExtensions", out var pe),
            "Expected camelCase property promptExtensions on PersonaDto.");
        Assert.Equal(JsonValueKind.Object, pe.ValueKind);
    }
}
