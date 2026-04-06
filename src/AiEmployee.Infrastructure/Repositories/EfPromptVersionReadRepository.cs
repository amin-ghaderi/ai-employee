using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfPromptVersionReadRepository : IPromptVersionReadRepository
{
    private readonly AiEmployeeDbContext _db;

    public EfPromptVersionReadRepository(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetMaxVersionAsync(Guid personaId, PromptType promptType, CancellationToken cancellationToken = default)
    {
        var max = await _db.PromptVersions
            .AsNoTracking()
            .Where(v => v.PersonaId == personaId && v.PromptType == promptType)
            .Select(v => (int?)v.Version)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);

        return max ?? 0;
    }
}
