namespace AiEmployee.Application.Prompting;

/// <summary>
/// Extracts a single JSON object substring from model output (handles optional markdown fences and leading prose).
/// </summary>
public static class ChatAssistantJsonPayloadExtractor
{
    public static bool TryExtractJsonObject(string assistantText, out string jsonObject)
    {
        jsonObject = string.Empty;
        if (string.IsNullOrWhiteSpace(assistantText))
            return false;

        var s = assistantText.Trim();

        if (s.StartsWith("```", StringComparison.Ordinal))
        {
            var close = s.LastIndexOf("```", StringComparison.Ordinal);
            if (close > 3)
            {
                s = s[3..close].Trim();
                var nl = s.IndexOf('\n');
                if (nl >= 0)
                {
                    var firstLine = s[..nl].Trim();
                    if (firstLine.Equals("json", StringComparison.OrdinalIgnoreCase)
                        || firstLine.StartsWith("json ", StringComparison.OrdinalIgnoreCase))
                    {
                        s = s[(nl + 1)..].Trim();
                    }
                }
            }
        }

        var start = s.IndexOf('{');
        var end = s.LastIndexOf('}');
        if (start < 0 || end <= start)
            return false;

        jsonObject = s.Substring(start, end - start + 1);
        return true;
    }
}
