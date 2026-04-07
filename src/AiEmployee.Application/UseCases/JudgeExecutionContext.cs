namespace AiEmployee.Application.UseCases;

public sealed class JudgeExecutionContext
{
    public string Prompt { get; init; } = string.Empty;
    public string PromptHash { get; init; } = string.Empty;
    public string PathType { get; init; } = "SIMPLE";
    public string Transcript { get; init; } = string.Empty;
}
