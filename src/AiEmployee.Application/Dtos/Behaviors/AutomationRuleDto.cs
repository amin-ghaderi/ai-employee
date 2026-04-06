using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Dtos.Behaviors;

public sealed class AutomationRuleDto
{
    public string TriggerTag { get; set; } = string.Empty;
    public string? SuppressIfTagPresent { get; set; }
    public AutomationActionKind Action { get; set; }
    public string? MarkTagOnFire { get; set; }
}
