using AiEmployee.Application.Interfaces;

namespace AiEmployee.Infrastructure.Messaging;

public sealed class NoOpMessageEmbeddingPublisher : IMessageEmbeddingPublisher
{
    public Task EnqueueAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
