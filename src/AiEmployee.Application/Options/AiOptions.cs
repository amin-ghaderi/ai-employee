namespace AiEmployee.Application.Options;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public bool UseFullJudgePrompt { get; set; }

    public string BaseUrl { get; set; } = "http://localhost:8000";
}
