using AiEmployee.Application.Dtos.Bots;

namespace AiEmployee.Application.Bots;

public interface IBotAdminService
{
    Task<BotDto> CreateAsync(CreateBotRequest request, CancellationToken cancellationToken = default);

    Task<BotDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BotDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<BotDto> UpdateAsync(Guid id, UpdateBotRequest request, CancellationToken cancellationToken = default);

    Task<BotDto> AssignAsync(Guid id, BotAssignmentsRequest request, CancellationToken cancellationToken = default);

    Task<BotDto> EnableAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BotDto> DisableAsync(Guid id, CancellationToken cancellationToken = default);
}
