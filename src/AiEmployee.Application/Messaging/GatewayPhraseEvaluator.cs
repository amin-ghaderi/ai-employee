using System.Text.RegularExpressions;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Messaging;

public sealed class GatewayPhraseEvaluator : IGatewayPhraseEvaluator
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

    public bool ShouldRouteToGateway(Behavior behavior, string messageText)
    {
        if (behavior is null || !behavior.EnableGatewayRouting)
            return false;

        if (string.IsNullOrWhiteSpace(messageText) ||
            string.IsNullOrWhiteSpace(behavior.GatewayTriggerPhrases))
            return false;

        var phrases = behavior.GatewayTriggerPhrases
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (phrases.Length == 0)
            return false;

        var comparison = behavior.GatewayCaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return behavior.GatewayMatchType switch
        {
            GatewayPhraseMatchType.Contains =>
                phrases.Any(p => messageText.Contains(p, comparison)),

            GatewayPhraseMatchType.Exact =>
                phrases.Any(p => string.Equals(messageText, p, comparison)),

            GatewayPhraseMatchType.Regex =>
                phrases.Any(p => Regex.IsMatch(
                    messageText,
                    p,
                    behavior.GatewayCaseSensitive
                        ? RegexOptions.None
                        : RegexOptions.IgnoreCase,
                    RegexTimeout)),

            _ => false
        };
    }
}
