using AiEmployee.Application.Integrations;
using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfBotIntegrationRepository : IBotIntegrationRepository
{
    private readonly AiEmployeeDbContext _db;

    public EfBotIntegrationRepository(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public async Task<BotIntegration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.BotIntegrations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<BotIntegration?> GetByChannelAndExternalIdAsync(
        string channel,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(externalId))
            return null;

        var normalizedChannel = channel.Trim().ToLowerInvariant();
        var trimmedExternalId = externalId.Trim();

        return await _db.BotIntegrations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                i =>
                    i.Channel.Trim().ToLower() == normalizedChannel &&
                    i.ExternalId == trimmedExternalId &&
                    i.IsEnabled,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BotIntegration>> ListAsync(Guid? botId, CancellationToken cancellationToken = default)
    {
        var query = _db.BotIntegrations.AsNoTracking().AsQueryable();
        if (botId is Guid bid)
            query = query.Where(i => i.BotId == bid);

        var list = await query
            .OrderBy(i => i.Channel)
            .ThenBy(i => i.ExternalId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list;
    }

    public async Task AddAsync(BotIntegration integration, CancellationToken cancellationToken = default)
    {
        _db.BotIntegrations.Add(integration);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            throw new BotIntegrationValidationException(new[]
            {
                "An integration with this channel and external id already exists.",
            });
        }
    }

    public async Task UpdateAsync(BotIntegration integration, CancellationToken cancellationToken = default)
    {
        var existing = await _db.BotIntegrations
            .FirstOrDefaultAsync(i => i.Id == integration.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No bot integration was found for id '{integration.Id}'.");

        existing.Update(
            integration.BotId,
            integration.Channel,
            integration.ExternalId,
            integration.IsEnabled,
            integration.GatewayChannel,
            integration.GatewayExternalId);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            throw new BotIntegrationValidationException(new[]
            {
                "An integration with this channel and external id already exists.",
            });
        }
    }
}
