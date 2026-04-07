namespace AiEmployee.Application.Admin;

public sealed class JudgeExecutionResult
{
    public string Winner { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    public PromptDebugResponse Debug { get; set; } = new();
}
