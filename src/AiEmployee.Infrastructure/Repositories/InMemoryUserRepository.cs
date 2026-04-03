using System.Collections.Concurrent;
using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.Entities;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _store = new();

    public Task<User?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task SaveAsync(User user)
    {
        _store[user.Id] = user;
        return Task.CompletedTask;
    }
}
