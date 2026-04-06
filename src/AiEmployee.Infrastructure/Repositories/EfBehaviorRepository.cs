using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfBehaviorRepository : IBehaviorRepository
{
    private readonly AiEmployeeDbContext _db;

    public EfBehaviorRepository(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public async Task<Behavior?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Behaviors
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Behavior>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Behaviors
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list;
    }

    public async Task AddAsync(Behavior behavior, CancellationToken cancellationToken = default)
    {
        _db.Behaviors.Add(behavior);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Behavior behavior, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Behaviors
            .FirstOrDefaultAsync(b => b.Id == behavior.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No behavior was found for id '{behavior.Id}'.");

        existing.ReplaceConfiguration(
            behavior.JudgeContextMessageCount,
            behavior.JudgePerMessageMaxChars,
            behavior.JudgeCommandPrefix,
            behavior.ExcludeCommandsFromJudgeContext,
            behavior.OnboardingFirstMessageOnly,
            behavior.LeadFlow,
            behavior.AutomationRules,
            behavior.EngagementRules,
            behavior.HotLeadPotentialValue,
            behavior.HotLeadTag,
            behavior.EnableChat,
            behavior.EnableLead,
            behavior.EnableJudge);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
