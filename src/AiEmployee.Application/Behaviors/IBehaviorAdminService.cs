using AiEmployee.Application.Dtos.Behaviors;

namespace AiEmployee.Application.Behaviors;

public interface IBehaviorAdminService
{
    Task<BehaviorDto> CreateAsync(CreateBehaviorRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BehaviorDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<BehaviorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BehaviorDto> UpdateAsync(Guid id, UpdateBehaviorRequest request, CancellationToken cancellationToken = default);
}
