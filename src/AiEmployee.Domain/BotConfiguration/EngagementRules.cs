namespace AiEmployee.Domain.BotConfiguration;

/// <summary>
/// Thresholds and sticky tags for user engagement tagging (policy input; application of rules is outside this type).
/// </summary>
public sealed class EngagementRules
{
    public int NewUserWindowHours { get; private set; }
    public int ActiveMessageThreshold { get; private set; }
    public int InactiveHoursThreshold { get; private set; }
    public double HighEngagementScoreThreshold { get; private set; }
    public double EngagementNormalizationFactor { get; private set; }
    public IReadOnlyList<string> StickyTags { get; private set; } = Array.Empty<string>();

    private EngagementRules()
    {
    }

    public EngagementRules(
        int newUserWindowHours,
        int activeMessageThreshold,
        int inactiveHoursThreshold,
        double highEngagementScoreThreshold,
        double engagementNormalizationFactor,
        IReadOnlyList<string> stickyTags)
    {
        NewUserWindowHours = newUserWindowHours;
        ActiveMessageThreshold = activeMessageThreshold;
        InactiveHoursThreshold = inactiveHoursThreshold;
        HighEngagementScoreThreshold = highEngagementScoreThreshold;
        EngagementNormalizationFactor = engagementNormalizationFactor;
        StickyTags = stickyTags ?? Array.Empty<string>();
    }
}
