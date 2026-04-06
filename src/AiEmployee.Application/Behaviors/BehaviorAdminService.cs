using AiEmployee.Application.Dtos.Behaviors;
using AiEmployee.Application.Interfaces;

namespace AiEmployee.Application.Behaviors;

public sealed class BehaviorAdminService : IBehaviorAdminService
{
    private readonly IBehaviorRepository _repository;

    public BehaviorAdminService(IBehaviorRepository repository)
    {
        _repository = repository;
    }

    public async Task<BehaviorDto> CreateAsync(
        CreateBehaviorRequest request,
        CancellationToken cancellationToken = default)
    {
        BehaviorRequestValidator.Validate(request);
        var id = Guid.NewGuid();
        var behavior = BehaviorMapper.ToDomain(id, request);
        await _repository.AddAsync(behavior, cancellationToken).ConfigureAwait(false);
        return BehaviorMapper.ToDto(behavior);
    }

    public async Task<IReadOnlyList<BehaviorDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return list.Select(BehaviorMapper.ToDto).ToList();
    }

    public async Task<BehaviorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var behavior = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return behavior is null ? null : BehaviorMapper.ToDto(behavior);
    }

    public async Task<BehaviorDto> UpdateAsync(
        Guid id,
        UpdateBehaviorRequest request,
        CancellationToken cancellationToken = default)
    {
        BehaviorRequestValidator.Validate(request);
        var existing = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No behavior was found for id '{id}'.");

        var behavior = BehaviorMapper.ToDomain(id, request);
        await _repository.UpdateAsync(behavior, cancellationToken).ConfigureAwait(false);
        return BehaviorMapper.ToDto(behavior);
    }
}
