using System.Collections.Concurrent;
using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.Entities;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class InMemoryLeadRepository : ILeadRepository
{
    private readonly ConcurrentDictionary<string, List<Lead>> _store = new();

    public Task SaveAsync(Lead lead)
    {
        var list = _store.GetOrAdd(lead.UserId, _ => new List<Lead>());
        lock (list)
        {
            list.Add(lead);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<Lead>> GetByUserIdAsync(string userId)
    {
        if (!_store.TryGetValue(userId, out var list))
            return Task.FromResult<IEnumerable<Lead>>(Array.Empty<Lead>());

        lock (list)
        {
            return Task.FromResult<IEnumerable<Lead>>(list.ToList());
        }
    }
}
