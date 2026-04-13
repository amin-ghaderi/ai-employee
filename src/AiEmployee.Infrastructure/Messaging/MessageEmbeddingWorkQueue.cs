using System.Threading.Channels;

namespace AiEmployee.Infrastructure.Messaging;

/// <summary>
/// In-process queue of message ids to index for vector search (PostgreSQL deployments only).
/// </summary>
public sealed class MessageEmbeddingWorkQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false,
    });

    public ChannelWriter<Guid> Writer => _channel.Writer;

    public ChannelReader<Guid> Reader => _channel.Reader;
}
