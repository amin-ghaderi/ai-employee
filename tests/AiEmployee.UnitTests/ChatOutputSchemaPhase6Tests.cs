using System.Text.Json;
using AiEmployee.Application.Prompting;
using AiEmployee.Infrastructure.AI;

namespace AiEmployee.UnitTests;

public sealed class ChatOutputSchemaPhase6Tests
{
    private static readonly string SimpleMessageSchema = """
        {"type":"object","required":["message"],"properties":{"message":{"type":"string"}}}
        """;

    [Fact]
    public void JsonPayloadExtractor_finds_object_after_prose()
    {
        Assert.True(ChatAssistantJsonPayloadExtractor.TryExtractJsonObject(
            "Here you go:\n{\"message\":\"ok\"}",
            out var json));
        Assert.Contains("\"message\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonPayloadExtractor_strips_markdown_fence()
    {
        const string fenced = """
            ```json
            {"message":"from fence"}
            ```
            """;
        Assert.True(ChatAssistantJsonPayloadExtractor.TryExtractJsonObject(fenced, out var json));
        Assert.Contains("from fence", json, StringComparison.Ordinal);
    }

    [Fact]
    public void StructuredResponseFormatter_prefers_message_property()
    {
        using var doc = JsonDocument.Parse("""{"message":"visible","other":1}""");
        var text = ChatStructuredResponseFormatter.ToUserVisibleText(doc.RootElement);
        Assert.Equal("visible", text);
    }

    [Fact]
    public void JsonSchemaChatOutputValidator_accepts_matching_instance()
    {
        var v = new JsonSchemaChatOutputValidator();
        var err = v.TryValidate("""{"message":"hello"}""", SimpleMessageSchema);
        Assert.Null(err);
    }

    [Fact]
    public void JsonSchemaChatOutputValidator_rejects_wrong_type()
    {
        var v = new JsonSchemaChatOutputValidator();
        var err = v.TryValidate("""{"message":3}""", SimpleMessageSchema);
        Assert.NotNull(err);
    }
}
