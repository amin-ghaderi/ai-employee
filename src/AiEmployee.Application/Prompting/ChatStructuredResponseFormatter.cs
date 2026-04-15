using System.Text.Json;

namespace AiEmployee.Application.Prompting;

/// <summary>
/// Turns a validated JSON instance into user-visible text. Prefer common string property names when present.
/// </summary>
public static class ChatStructuredResponseFormatter
{
    private static readonly string[] PreferredStringPropertyNames =
    {
        "message", "reply", "text", "content", "response", "answer",
    };

    public static string ToUserVisibleText(JsonElement root)
    {
        return root.ValueKind switch
        {
            JsonValueKind.String => root.GetString() ?? string.Empty,
            JsonValueKind.Object => TryExtractPreferredString(root) ?? CompactSerialize(root),
            JsonValueKind.Array => CompactSerialize(root),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => root.GetRawText(),
            _ => root.GetRawText(),
        };
    }

    private static string? TryExtractPreferredString(JsonElement obj)
    {
        foreach (var name in PreferredStringPropertyNames)
        {
            if (!obj.TryGetProperty(name, out var p))
                continue;
            if (p.ValueKind == JsonValueKind.String)
                return p.GetString();
        }

        return null;
    }

    private static string CompactSerialize(JsonElement el)
    {
        return JsonSerializer.Serialize(el, JsonSerializerOptionsCache.Compact);
    }

    private static class JsonSerializerOptionsCache
    {
        public static readonly JsonSerializerOptions Compact = new()
        {
            WriteIndented = false,
        };
    }
}
