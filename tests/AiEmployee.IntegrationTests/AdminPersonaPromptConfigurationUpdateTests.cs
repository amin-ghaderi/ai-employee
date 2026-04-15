using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiEmployee.IntegrationTests;

/// <summary>Validates Prompt Configuration (Persona) API round-trip for <c>promptExtensions</c> after Phase 3.</summary>
public sealed class AdminPersonaPromptConfigurationUpdateTests : IClassFixture<PostgresWebApplicationFactory>, IDisposable
{
    private const string ValidAdminKey = "your-secret-key";

    private readonly PostgresWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminPersonaPromptConfigurationUpdateTests(PostgresWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Put_persona_roundtrips_chat_output_schema_in_prompt_extensions()
    {
        var personaId = JudgeBotDefaults.PersonaId;

        using var get0 = new HttpRequestMessage(HttpMethod.Get, $"/admin/personas/{personaId}");
        get0.Headers.Add("X-Admin-Key", ValidAdminKey);
        var originalRes = await _client.SendAsync(get0);
        Assert.Equal(HttpStatusCode.OK, originalRes.StatusCode);
        var originalJson = await originalRes.Content.ReadAsStringAsync();

        try
        {
            var root = JsonNode.Parse(originalJson)!;
            var px = root["promptExtensions"] as JsonObject ?? new JsonObject();
            px["chatOutputSchemaJson"] =
                """{"type":"object","properties":{"phase3Test":{"type":"boolean"}}}""";
            px["judgeInstruction"] = "Decide using {{input}} and return JSON.";
            px["judgeSchemaJson"] = """{"type":"object"}""";
            px["leadInstruction"] = "Classify using {{goal}} and {{experience}}.";
            px["leadSchemaJson"] = """{"type":"object"}""";
            root["promptExtensions"] = px;

            using var put = new HttpRequestMessage(HttpMethod.Put, $"/admin/personas/{personaId}");
            put.Headers.Add("X-Admin-Key", ValidAdminKey);
            put.Content = new StringContent(
                root.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
                Encoding.UTF8,
                "application/json");

            var putRes = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);

            using var get1 = new HttpRequestMessage(HttpMethod.Get, $"/admin/personas/{personaId}");
            get1.Headers.Add("X-Admin-Key", ValidAdminKey);
            var read = await _client.SendAsync(get1);
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            using var doc = await JsonDocument.ParseAsync(await read.Content.ReadAsStreamAsync());
            var pe = doc.RootElement.GetProperty("promptExtensions");
            Assert.True(pe.GetProperty("chatOutputSchemaJson").GetString()?.Contains("phase3Test", StringComparison.Ordinal) == true);
            Assert.Contains("{{input}}", pe.GetProperty("judgeInstruction").GetString() ?? "", StringComparison.Ordinal);
        }
        finally
        {
            using var restore = new HttpRequestMessage(HttpMethod.Put, $"/admin/personas/{personaId}");
            restore.Headers.Add("X-Admin-Key", ValidAdminKey);
            restore.Content = new StringContent(originalJson, Encoding.UTF8, "application/json");
            await _client.SendAsync(restore);
        }
    }

    [Fact]
    public async Task Put_persona_prompt_extensions_appends_prompt_version_rows_per_extension_prompt_type()
    {
        var personaId = JudgeBotDefaults.PersonaId;
        var marker = Guid.NewGuid().ToString("N");

        using var get0 = new HttpRequestMessage(HttpMethod.Get, $"/admin/personas/{personaId}");
        get0.Headers.Add("X-Admin-Key", ValidAdminKey);
        var originalRes = await _client.SendAsync(get0);
        Assert.Equal(HttpStatusCode.OK, originalRes.StatusCode);
        var originalJson = await originalRes.Content.ReadAsStringAsync();

        async Task<int> MaxVersionAsync(PromptType promptType)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AiEmployeeDbContext>();
            return await db.PromptVersions.AsNoTracking()
                .Where(v => v.PersonaId == personaId && v.PromptType == promptType)
                .OrderByDescending(v => v.Version)
                .Select(v => v.Version)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        var beforeChatSchema = await MaxVersionAsync(PromptType.ChatOutputSchema);
        var beforeJudgeInstr = await MaxVersionAsync(PromptType.JudgeInstruction);
        var beforeJudgeSchema = await MaxVersionAsync(PromptType.JudgeSchema);
        var beforeLeadInstr = await MaxVersionAsync(PromptType.LeadInstruction);
        var beforeLeadSchema = await MaxVersionAsync(PromptType.LeadSchema);

        try
        {
            var root = JsonNode.Parse(originalJson)!;
            var px = root["promptExtensions"] as JsonObject ?? new JsonObject();
            px["chatOutputSchemaJson"] =
                "{\"type\":\"object\",\"properties\":{\"" + marker + "\":{\"type\":\"string\"}}}";
            px["judgeInstruction"] = $"Decide using {{input}}. Marker={marker}.";
            px["judgeSchemaJson"] =
                "{\"type\":\"object\",\"properties\":{\"" + marker + "\":{\"type\":\"boolean\"}}}";
            px["leadInstruction"] = $"Classify using {{goal}}. Marker={marker}.";
            px["leadSchemaJson"] =
                "{\"type\":\"object\",\"properties\":{\"" + marker + "\":{\"type\":\"integer\"}}}";
            root["promptExtensions"] = px;

            using var put = new HttpRequestMessage(HttpMethod.Put, $"/admin/personas/{personaId}");
            put.Headers.Add("X-Admin-Key", ValidAdminKey);
            put.Content = new StringContent(
                root.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
                Encoding.UTF8,
                "application/json");

            var putRes = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);

            Assert.True(await MaxVersionAsync(PromptType.ChatOutputSchema) > beforeChatSchema);
            Assert.True(await MaxVersionAsync(PromptType.JudgeInstruction) > beforeJudgeInstr);
            Assert.True(await MaxVersionAsync(PromptType.JudgeSchema) > beforeJudgeSchema);
            Assert.True(await MaxVersionAsync(PromptType.LeadInstruction) > beforeLeadInstr);
            Assert.True(await MaxVersionAsync(PromptType.LeadSchema) > beforeLeadSchema);
        }
        finally
        {
            using var restore = new HttpRequestMessage(HttpMethod.Put, $"/admin/personas/{personaId}");
            restore.Headers.Add("X-Admin-Key", ValidAdminKey);
            restore.Content = new StringContent(originalJson, Encoding.UTF8, "application/json");
            await _client.SendAsync(restore);
        }
    }
}
