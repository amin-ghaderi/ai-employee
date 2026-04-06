using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Interfaces;

public interface IBotRepository
{
    Task<Bot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Bot>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Bot bot, CancellationToken cancellationToken = default);

    Task UpdateAsync(Bot bot, CancellationToken cancellationToken = default);
}
