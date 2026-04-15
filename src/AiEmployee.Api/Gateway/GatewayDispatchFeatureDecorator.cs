using AiEmployee.Application.Messaging;
using Microsoft.FeatureManagement;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Api.Gateway;

/// <summary>
/// Feature-flag gate for <see cref="IGatewayDispatcher"/> (Api-only; keeps Application free of FeatureManagement).
/// </summary>
public sealed class GatewayDispatchFeatureDecorator : IGatewayDispatcher
{
    public const string FeatureName = "GatewayDispatch";

    private readonly IFeatureManager _featureManager;
    private readonly GatewayDispatcher _inner;
    private readonly IGatewayTelemetry _telemetry;
    private readonly ILogger<GatewayDispatchFeatureDecorator> _logger;

    public GatewayDispatchFeatureDecorator(
        IFeatureManager featureManager,
        GatewayDispatcher inner,
        IGatewayTelemetry telemetry,
        ILogger<GatewayDispatchFeatureDecorator> logger)
    {
        _featureManager = featureManager;
        _inner = inner;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task DispatchAsync(
        Guid botId,
        string inboundChannel,
        string inboundExternalId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!await _featureManager.IsEnabledAsync(FeatureName, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation(
                "Gateway dispatch skipped: feature {FeatureName} is disabled",
                FeatureName);
            _telemetry.RecordDispatchOutcome("feature_disabled");
            return;
        }

        await _inner
            .DispatchAsync(botId, inboundChannel, inboundExternalId, message, cancellationToken)
            .ConfigureAwait(false);
    }
}
