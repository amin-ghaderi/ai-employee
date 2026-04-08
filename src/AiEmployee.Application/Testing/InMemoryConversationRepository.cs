using System.Collections.Concurrent;
using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Testing;

public sealed class InMemoryConversationRepository : IConversationRepository
{
    private readonly ConcurrentDictionary<string, Conversation> _store = new();

    public Task<Conversation?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var conversation);
        return Task.FromResult(conversation);
    }

    public Task SaveAsync(Conversation conversation)
    {
        _store[conversation.Id] = conversation;
        return Task.CompletedTask;
    }

    public Task ReplaceMessagesAsync(
        string conversationId,
        IReadOnlyList<Message> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var conv = _store.GetOrAdd(conversationId, _ => new Conversation(conversationId));

        conv.Messages.Clear();
        foreach (var m in messages)
            conv.AddMessage(m);

        return Task.CompletedTask;
    }
}
