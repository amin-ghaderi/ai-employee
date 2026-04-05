using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Services;

public sealed class UserTaggingService
{
    public void Apply(User user, EngagementRules rules)
    {
        var sticky = user.Tags
            .Where(t => rules.StickyTags.Contains(t, StringComparer.Ordinal))
            .Distinct()
            .ToList();

        user.Tags.Clear();

        var now = DateTime.UtcNow;
        var days = (now - user.JoinedAt).TotalDays;

        double engagementScore;
        if (days <= 0)
            engagementScore = user.MessagesCount > 0 ? 1.0 : 0.0;
        else
        {
            var messagesPerDay = user.MessagesCount / days;
            engagementScore = Math.Min(1.0, messagesPerDay / rules.EngagementNormalizationFactor);
        }

        if ((now - user.JoinedAt).TotalHours < rules.NewUserWindowHours)
            user.Tags.Add("new");

        if (user.MessagesCount >= rules.ActiveMessageThreshold)
            user.Tags.Add("active");

        if ((now - user.LastActiveAt).TotalHours > rules.InactiveHoursThreshold)
            user.Tags.Add("inactive");

        if (engagementScore > rules.HighEngagementScoreThreshold)
            user.Tags.Add("high_engagement");

        foreach (var t in sticky)
            user.Tags.Add(t);
    }
}
