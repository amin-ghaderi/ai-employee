namespace AiEmployee.Application.Messaging;

public interface IGatewayDispatcher
{
    Task DispatchAsync(
        Guid botId,
        string inboundChannel,
        string inboundExternalId,
        string message,
        CancellationToken cancellationToken = default);
}
