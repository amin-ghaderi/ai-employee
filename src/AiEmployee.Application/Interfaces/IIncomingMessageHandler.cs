using AiEmployee.Application.Messaging;

namespace AiEmployee.Application.Interfaces;

public interface IIncomingMessageHandler
{
    Task HandleAsync(IncomingMessage message, CancellationToken cancellationToken = default);
}
