namespace AiEmployee.Application.Options;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>When true, judge uses <see cref="PromptBuilder.BuildFullJudgePrompt"/> when conversation exists; otherwise falls back to simple text path.</summary>
    public bool UseFullJudgePrompt { get; set; } = true;

    public string BaseUrl { get; set; } = "http://localhost:8000";
}
