using System.Diagnostics;
using AiEmployee.Application.Messaging;

namespace AiEmployee.Infrastructure.Messaging;

public sealed class GatewayTelemetryRecorder : IGatewayTelemetry
{
    private sealed class ActivityScope : IDisposable
    {
        private readonly Activity? _activity;

        public ActivityScope(Activity? activity) => _activity = activity;

        public void Dispose() => _activity?.Dispose();
    }

    public void RecordPhraseEvaluation(bool matched)
    {
        GatewayTelemetryPrimitives.PhraseEvaluations.Add(
            1,
            new KeyValuePair<string, object?>("matched", matched));
    }

    public IDisposable StartGatewayHandlingActivity(Guid botId, Guid behaviorId, string inboundChannel)
    {
        var activity = GatewayTelemetryPrimitives.ActivitySource.StartActivity(
            "Gateway.Handle",
            ActivityKind.Internal,
            parentContext: default,
            tags:
            [
                new("gateway.bot_id", botId.ToString("D")),
                new("gateway.behavior_id", behaviorId.ToString("D")),
                new("messaging.channel.inbound", TruncateChannel(inboundChannel)),
            ]);
        return new ActivityScope(activity);
    }

    public void RecordDispatchAttempt()
    {
        GatewayTelemetryPrimitives.DispatchAttempts.Add(1);
    }

    public void RecordDispatchOutcome(string outcome)
    {
        GatewayTelemetryPrimitives.DispatchOutcomes.Add(
            1,
            new KeyValuePair<string, object?>("outcome", outcome));
    }

    private static string TruncateChannel(string channel)
    {
        if (string.IsNullOrEmpty(channel)) return "";
        var t = channel.Trim();
        return t.Length <= 64 ? t : t[..64];
    }
}
