using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Services;

public sealed class AutomationService
{
    public IEnumerable<string> Evaluate(User user)
    {
        var actions = new List<string>();

        // Rule 1: inactive user (once per condition)
        if (user.Tags.Contains("inactive") && !user.Tags.Contains("inactive_notified"))
        {
            actions.Add("send_reactivation_message");
            user.Tags.Add("inactive_notified");
        }

        // Rule 2: high engagement (once per condition)
        if (user.Tags.Contains("high_engagement") && !user.Tags.Contains("high_engagement_notified"))
        {
            actions.Add("notify_admin_high_engagement");
            user.Tags.Add("high_engagement_notified");
        }

        return actions;
    }
}
