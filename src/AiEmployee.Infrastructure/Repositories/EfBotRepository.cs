using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfBotRepository : IBotRepository
{
    private readonly AiEmployeeDbContext _db;

    public EfBotRepository(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public async Task<Bot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Bots
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Bot>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Bots
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list;
    }

    public async Task AddAsync(Bot bot, CancellationToken cancellationToken = default)
    {
        _db.Bots.Add(bot);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Bot bot, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Bots
            .FirstOrDefaultAsync(b => b.Id == bot.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No bot was found for id '{bot.Id}'.");

        existing.Update(bot.Name, bot.IsEnabled, bot.UpdatedAt ?? DateTimeOffset.UtcNow);
        existing.Assign(bot.PersonaId, bot.BehaviorId, bot.LanguageProfileId, bot.UpdatedAt ?? DateTimeOffset.UtcNow);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
