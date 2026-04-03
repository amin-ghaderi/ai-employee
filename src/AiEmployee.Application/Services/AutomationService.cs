using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Services;

public sealed class AutomationService
{
    public IEnumerable<string> Evaluate(User user)
    {
        var actions = new List<string>();

        // Rule 1: inactive user
        if (user.Tags.Contains("inactive"))
        {
            actions.Add("send_reactivation_message");
        }

        // Rule 2: high engagement
        if (user.Tags.Contains("high_engagement"))
        {
            actions.Add("notify_admin_high_engagement");
        }

        return actions;
    }
}
