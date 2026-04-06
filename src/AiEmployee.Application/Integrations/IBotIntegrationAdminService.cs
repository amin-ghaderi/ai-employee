using AiEmployee.Application.Dtos.Integrations;

namespace AiEmployee.Application.Integrations;

public interface IBotIntegrationAdminService
{
    Task<BotIntegrationDto> CreateAsync(CreateBotIntegrationRequest request, CancellationToken cancellationToken = default);

    Task<BotIntegrationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BotIntegrationDto>> ListAsync(Guid? botId, CancellationToken cancellationToken = default);

    Task<BotIntegrationDto> UpdateAsync(Guid id, UpdateBotIntegrationRequest request, CancellationToken cancellationToken = default);

    Task<BotIntegrationDto> EnableAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BotIntegrationDto> DisableAsync(Guid id, CancellationToken cancellationToken = default);
}
