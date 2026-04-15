namespace AiEmployee.Application.Options;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>When true, judge uses <see cref="PromptBuilder.BuildFullJudgePrompt"/> when conversation exists; otherwise falls back to simple text path.</summary>
    public bool UseFullJudgePrompt { get; set; } = true;

    /// <summary>
    /// When true (default) and the persona defines <c>ChatOutputSchemaJson</c>, chat completions must parse as JSON and satisfy that schema.
    /// Set false only for emergency rollback without redeploying prompt configuration.
    /// </summary>
    public bool EnforceChatOutputSchema { get; set; } = true;

    public string BaseUrl { get; set; } = "http://localhost:8000";
}
