using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.Entities;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfUserRepository : IUserRepository
{
    private readonly AiEmployeeDbContext _db;

    public EfUserRepository(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task SaveAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (existing is null)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return;
        }

        var tagsCopy = user.Tags.ToList();
        _db.Entry(existing).CurrentValues.SetValues(user);

        existing.Tags.Clear();
        foreach (var tag in tagsCopy)
            existing.Tags.Add(tag);

        await _db.SaveChangesAsync();
    }
}
