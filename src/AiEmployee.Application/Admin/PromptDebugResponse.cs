namespace AiEmployee.Application.Admin;

public class BaseDebugResponse
{
    public string? Prompt { get; set; }
    public string? PromptSource { get; set; }
    public object? Schema { get; set; }
    public object? ParsedResult { get; set; }
    public string? RawResponse { get; set; }
    public long? LatencyMs { get; set; }
}

public class JudgeDebugResponse : BaseDebugResponse
{
    public bool HasInputToken { get; set; }
    public bool HasGoalToken { get; set; }
    public bool HasExperienceToken { get; set; }

    public string? PathType { get; set; }

    public string BotId { get; set; } = string.Empty;
    public string PersonaId { get; set; } = string.Empty;
    public string BehaviorId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? PromptHash { get; set; }
}

public class LeadDebugResponse : BaseDebugResponse
{
    public Guid? BotId { get; set; }
    public Guid? PersonaId { get; set; }
    public Guid? BehaviorId { get; set; }
    public string? Channel { get; set; }
}

public sealed class PromptDebugResponse : JudgeDebugResponse
{
}
