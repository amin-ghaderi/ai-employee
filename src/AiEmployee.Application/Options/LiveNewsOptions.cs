namespace AiEmployee.Application.Options;

/// <summary>
/// Google News RSS live headlines for the assistant (off by default).
/// </summary>
public sealed class LiveNewsOptions
{
    public const string SectionName = "LiveNews";

    public bool Enabled { get; set; }

    /// <summary>Number of headlines to include (clamped to 3–5).</summary>
    public int MaxHeadlines { get; set; } = 5;

    public int MaxTitleChars { get; set; } = 120;

    public string Language { get; set; } = "en";

    public string Region { get; set; } = "US";

    public int RequestTimeoutSeconds { get; set; } = 10;

    public string UserAgent { get; set; } = "AiEmployee/1.0 (LiveNews)";

    /// <summary>Case-insensitive substrings; if empty, a built-in default list is used.</summary>
    public string[] TriggerKeywords { get; set; } = Array.Empty<string>();
}
