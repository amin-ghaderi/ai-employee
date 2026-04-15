using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AiEmployee.Infrastructure.Messaging;

/// <summary>OTel Meter and ActivitySource for gateway routing (Infrastructure-only).</summary>
public static class GatewayTelemetryPrimitives
{
    public static readonly ActivitySource ActivitySource = new("AiEmployee.Gateway", "1.0.0");

    public static readonly Meter Meter = new("AiEmployee.Gateway", "1.0.0");

    public static readonly Counter<long> PhraseEvaluations =
        Meter.CreateCounter<long>(
            "gateway.phrase.evaluations",
            description: "Phrase evaluations when gateway routing is enabled on the behavior.");

    public static readonly Counter<long> DispatchOutcomes =
        Meter.CreateCounter<long>(
            "gateway.dispatch.outcomes",
            description: "Gateway dispatch results by outcome.");

    public static readonly Counter<long> DispatchAttempts =
        Meter.CreateCounter<long>(
            "gateway.dispatch.attempts",
            description: "Gateway dispatch attempts after phrase match.");
}
