namespace AiEmployee.Application.Admin;

public sealed class UpdateBotConfigRequest
{
    public string JudgePrompt { get; init; } = string.Empty;
    public string LeadPrompt { get; init; } = string.Empty;
}
