namespace AiEmployee.Domain.Entities;

/// <summary>
/// Result of evaluating a conversation or message (pure domain model).
/// </summary>
public sealed class JudgmentResult
{
    public string Winner { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
