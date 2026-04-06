using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Interfaces;

public interface ILanguageProfileRepository
{
    Task<LanguageProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
