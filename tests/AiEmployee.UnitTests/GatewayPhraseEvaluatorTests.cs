using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Application.Messaging;

namespace AiEmployee.UnitTests;

public sealed class GatewayPhraseEvaluatorTests
{
    private static readonly LeadFlow DefaultLead = new(null, null, Array.Empty<string>());
    private static readonly EngagementRules DefaultEngagement = new(
        48, 10, 72, 0.7, 1, Array.Empty<string>());

    private static Behavior CreateBehavior(
        bool enableGateway,
        string? phrases,
        GatewayPhraseMatchType matchType,
        bool caseSensitive = false) =>
        new(
            Guid.NewGuid(),
            5,
            2000,
            "/judge",
            false,
            false,
            DefaultLead,
            Array.Empty<AutomationRule>(),
            DefaultEngagement,
            "",
            "",
            enableChat: true,
            enableLead: true,
            enableJudge: true,
            enableGatewayRouting: enableGateway,
            gatewayTriggerPhrases: phrases,
            gatewayMatchType: matchType,
            gatewayCaseSensitive: caseSensitive);

    [Fact]
    public void ShouldRouteToGateway_returns_false_when_routing_disabled()
    {
        var sut = new GatewayPhraseEvaluator();
        var b = CreateBehavior(false, "help", GatewayPhraseMatchType.Contains);
        Assert.False(sut.ShouldRouteToGateway(b, "help"));
    }

    [Fact]
    public void Contains_matches_comma_separated_phrase()
    {
        var sut = new GatewayPhraseEvaluator();
        var b = CreateBehavior(true, "help, human", GatewayPhraseMatchType.Contains);
        Assert.True(sut.ShouldRouteToGateway(b, "I need help today"));
        Assert.False(sut.ShouldRouteToGateway(b, "hello"));
    }

    [Fact]
    public void Exact_matches_full_message_only()
    {
        var sut = new GatewayPhraseEvaluator();
        var b = CreateBehavior(true, "stop", GatewayPhraseMatchType.Exact);
        Assert.True(sut.ShouldRouteToGateway(b, "stop"));
        Assert.False(sut.ShouldRouteToGateway(b, "stop please"));
    }

    [Fact]
    public void Regex_mode_uses_pattern()
    {
        var sut = new GatewayPhraseEvaluator();
        var b = CreateBehavior(true, "^urgent\\b", GatewayPhraseMatchType.Regex);
        Assert.True(sut.ShouldRouteToGateway(b, "urgent help"));
        Assert.False(sut.ShouldRouteToGateway(b, "not urgent"));
    }

    [Fact]
    public void Empty_or_whitespace_message_never_matches()
    {
        var sut = new GatewayPhraseEvaluator();
        var b = CreateBehavior(true, "a", GatewayPhraseMatchType.Contains);
        Assert.False(sut.ShouldRouteToGateway(b, ""));
        Assert.False(sut.ShouldRouteToGateway(b, "   "));
    }
}
