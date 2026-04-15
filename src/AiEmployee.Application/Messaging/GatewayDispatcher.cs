using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Application.Messaging;

public sealed class GatewayDispatcher : IGatewayDispatcher
{
    private readonly IBotIntegrationRepository _integrationRepository;
    private readonly IOutgoingMessageClient _outgoingMessageClient;
    private readonly IGatewayTelemetry _gatewayTelemetry;
    private readonly IOptions<GatewayOptions> _gatewayOptions;
    private readonly ILogger<GatewayDispatcher> _logger;

    public GatewayDispatcher(
        IBotIntegrationRepository integrationRepository,
        IOutgoingMessageClient outgoingMessageClient,
        IGatewayTelemetry gatewayTelemetry,
        IOptions<GatewayOptions> gatewayOptions,
        ILogger<GatewayDispatcher> logger)
    {
        _integrationRepository = integrationRepository;
        _outgoingMessageClient = outgoingMessageClient;
        _gatewayTelemetry = gatewayTelemetry;
        _gatewayOptions = gatewayOptions;
        _logger = logger;
    }

    public async Task DispatchAsync(
        Guid botId,
        string inboundChannel,
        string inboundExternalId,
        string message,
        CancellationToken cancellationToken = default)
    {
        _gatewayTelemetry.RecordDispatchAttempt();

        var integration = await _integrationRepository
            .GetByChannelAndExternalIdAsync(
                inboundChannel,
                inboundExternalId,
                cancellationToken)
            .ConfigureAwait(false);

        if (integration is null)
        {
            _logger.LogWarning(
                "Gateway dispatch failed: integration not found | channel={Channel} inboundExternalIdLength={InboundExternalIdLength}",
                inboundChannel,
                inboundExternalId.Length);
            _gatewayTelemetry.RecordDispatchOutcome("integration_not_found");
            return;
        }

        if (!integration.IsEnabled)
        {
            _logger.LogWarning(
                "Gateway dispatch skipped: integration disabled | integrationId={IntegrationId}",
                integration.Id);
            _gatewayTelemetry.RecordDispatchOutcome("integration_disabled");
            return;
        }

        if (integration.BotId != botId)
        {
            _logger.LogWarning(
                "Gateway dispatch skipped: integration bot mismatch | integrationBotId={IntegrationBotId} botId={BotId}",
                integration.BotId,
                botId);
            _gatewayTelemetry.RecordDispatchOutcome("bot_mismatch");
            return;
        }

        if (string.IsNullOrWhiteSpace(integration.GatewayChannel) ||
            string.IsNullOrWhiteSpace(integration.GatewayExternalId))
        {
            _logger.LogInformation(
                "Gateway not configured | botId={BotId} integrationId={IntegrationId}",
                botId,
                integration.Id);
            _gatewayTelemetry.RecordDispatchOutcome("gateway_not_configured");
            return;
        }

        var maxChars = Math.Max(256, _gatewayOptions.Value.MaxForwardedMessageChars);
        var payload = message ?? string.Empty;
        var truncated = false;
        if (payload.Length > maxChars)
        {
            payload = payload[..maxChars];
            truncated = true;
        }

        try
        {
            await _outgoingMessageClient
                .SendMessageAsync(
                    integration.GatewayChannel,
                    integration.GatewayExternalId,
                    payload)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Gateway outbound send failed | botId={BotId} integrationId={IntegrationId} gatewayChannel={GatewayChannel}",
                botId,
                integration.Id,
                integration.GatewayChannel);
            _gatewayTelemetry.RecordDispatchOutcome("outbound_exception");
            throw;
        }

        _logger.LogInformation(
            "Gateway message dispatched | botId={BotId} integrationId={IntegrationId} gatewayChannel={GatewayChannel} textLength={TextLength} truncated={Truncated}",
            botId,
            integration.Id,
            integration.GatewayChannel,
            payload.Length,
            truncated);

        _gatewayTelemetry.RecordDispatchOutcome(truncated ? "success_truncated" : "success");
    }
}
