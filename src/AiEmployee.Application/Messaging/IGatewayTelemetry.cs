namespace AiEmployee.Application.Messaging;

/// <summary>
/// Gateway routing observability (metrics and tracing). Implementations live outside Domain;
/// Application code must not log message content or identifiers that identify end users (PII).
/// </summary>
public interface IGatewayTelemetry
{
    /// <summary>Recorded when <see cref="IGatewayPhraseEvaluator.ShouldRouteToGateway"/> runs and gateway routing is enabled on the behavior.</summary>
    void RecordPhraseEvaluation(bool matched);

    /// <summary>Starts a tracing scope for the gateway handling path (phrase matched through dispatch).</summary>
    IDisposable StartGatewayHandlingActivity(Guid botId, Guid behaviorId, string inboundChannel);

    void RecordDispatchAttempt();

    /// <summary>
    /// Outcome values: success, integration_not_found, integration_disabled, bot_mismatch, gateway_not_configured,
    /// outbound_exception, message_truncated (truncation is applied before send; send may still follow).
    /// </summary>
    void RecordDispatchOutcome(string outcome);
}
