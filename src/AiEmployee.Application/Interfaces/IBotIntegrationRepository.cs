using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Interfaces;

public interface IBotIntegrationRepository
{
    Task<BotIntegration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BotIntegration?> GetByChannelAndExternalIdAsync(
        string channel,
        string externalId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BotIntegration>> ListAsync(Guid? botId, CancellationToken cancellationToken = default);

    Task AddAsync(BotIntegration integration, CancellationToken cancellationToken = default);

    Task UpdateAsync(BotIntegration integration, CancellationToken cancellationToken = default);
}
