using AiEmployee.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Infrastructure.Messaging;

public sealed class ChannelMessageEmbeddingPublisher : IMessageEmbeddingPublisher
{
    private readonly MessageEmbeddingWorkQueue _queue;
    private readonly ILogger<ChannelMessageEmbeddingPublisher> _logger;

    public ChannelMessageEmbeddingPublisher(
        MessageEmbeddingWorkQueue queue,
        ILogger<ChannelMessageEmbeddingPublisher> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public Task EnqueueAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (!_queue.Writer.TryWrite(messageId))
        {
            _logger.LogWarning(
                "Could not enqueue message embedding job (channel closed or full) | messageId={MessageId}",
                messageId);
        }

        return Task.CompletedTask;
    }
}
