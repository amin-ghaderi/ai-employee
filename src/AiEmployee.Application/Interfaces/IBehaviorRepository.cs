using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Interfaces;

public interface IBehaviorRepository
{
    Task<Behavior?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Behavior>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Behavior behavior, CancellationToken cancellationToken = default);

    Task UpdateAsync(Behavior behavior, CancellationToken cancellationToken = default);
}
