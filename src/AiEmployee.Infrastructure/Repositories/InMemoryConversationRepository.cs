using System.Collections.Concurrent;
using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.Entities;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class InMemoryConversationRepository : IConversationRepository
{
    private readonly ConcurrentDictionary<string, Conversation> _store = new();

    public Task<Conversation?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var conversation);
        return Task.FromResult(conversation);
    }

    public Task<Message?> GetMessageByIdAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        lock (_store)
        {
            foreach (var conv in _store.Values)
            {
                var found = conv.Messages.FirstOrDefault(m => m.Id == messageId);
                if (found is not null)
                    return Task.FromResult<Message?>(found);
            }
        }

        return Task.FromResult<Message?>(null);
    }

    public Task SaveAsync(Conversation conversation)
    {
        _store[conversation.Id] = conversation;
        return Task.CompletedTask;
    }

    public Task AppendUserMessageAsync(string conversationId, Message message, CancellationToken cancellationToken = default) =>
        AppendMessageCoreAsync(conversationId, message);

    public Task AppendAssistantMessageAsync(string conversationId, Message message, CancellationToken cancellationToken = default) =>
        AppendMessageCoreAsync(conversationId, message);

    private Task AppendMessageCoreAsync(string conversationId, Message message)
    {
        lock (_store)
        {
            if (!_store.TryGetValue(conversationId, out var conv))
            {
                conv = new Conversation(conversationId);
                _store[conversationId] = conv;
            }

            if (!conv.Messages.Any(m => m.Id == message.Id))
                conv.AddMessage(message);
        }

        return Task.CompletedTask;
    }

    public Task ReplaceMessagesAsync(string conversationId, IReadOnlyList<Message> messages, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var conv = new Conversation(conversationId);
        foreach (var m in messages)
            conv.AddMessage(m);

        _store[conversationId] = conv;
        return Task.CompletedTask;
    }
}
