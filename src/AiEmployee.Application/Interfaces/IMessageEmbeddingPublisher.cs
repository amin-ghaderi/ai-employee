namespace AiEmployee.Application.Interfaces;

public interface IMessageEmbeddingPublisher
{
    Task EnqueueAsync(Guid messageId, CancellationToken cancellationToken = default);
}
