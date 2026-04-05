using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Services;

public sealed class AutomationService
{
    public IEnumerable<AutomationActionKind> Evaluate(User user, IReadOnlyList<AutomationRule> rules)
    {
        var actions = new List<AutomationActionKind>();

        foreach (var rule in rules)
        {
            if (!user.Tags.Contains(rule.TriggerTag))
                continue;

            if (rule.SuppressIfTagPresent is not null && user.Tags.Contains(rule.SuppressIfTagPresent))
                continue;

            if (rule.MarkTagOnFire is not null)
                user.Tags.Add(rule.MarkTagOnFire);

            actions.Add(rule.Action);
        }

        return actions;
    }
}
