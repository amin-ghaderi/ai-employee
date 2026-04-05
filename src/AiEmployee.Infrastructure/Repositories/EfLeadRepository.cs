using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.Entities;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfLeadRepository : ILeadRepository
{
    private readonly AiEmployeeDbContext _db;

    public EfLeadRepository(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Lead>> GetByUserIdAsync(string userId)
    {
        return await _db.Leads
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task SaveAsync(Lead lead)
    {
        ArgumentNullException.ThrowIfNull(lead);

        var alreadyExists = await _db.Leads.AnyAsync(l => l.UserId == lead.UserId);
        if (alreadyExists)
            return;

        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();
    }
}
