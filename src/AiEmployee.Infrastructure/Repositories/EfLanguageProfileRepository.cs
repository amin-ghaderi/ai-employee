using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfLanguageProfileRepository : ILanguageProfileRepository
{
    private readonly AiEmployeeDbContext _db;

    public EfLanguageProfileRepository(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public async Task<LanguageProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.LanguageProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }
}
