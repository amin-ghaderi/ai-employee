namespace AiEmployee.Application.Dtos.Behaviors;

public sealed class EngagementRulesDto
{
    public int NewUserWindowHours { get; set; }
    public int ActiveMessageThreshold { get; set; }
    public int InactiveHoursThreshold { get; set; }
    public double HighEngagementScoreThreshold { get; set; }
    public double EngagementNormalizationFactor { get; set; }
    public IReadOnlyList<string> StickyTags { get; set; } = Array.Empty<string>();
}
