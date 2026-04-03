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

    public Task SaveAsync(Conversation conversation)
    {
        _store[conversation.Id] = conversation;
        return Task.CompletedTask;
    }
}
