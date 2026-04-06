using AiEmployee.Application.Dtos.Personas;

namespace AiEmployee.Application.Personas;

public interface IPersonaAdminService
{
    Task<PersonaDto> CreateAsync(CreatePersonaRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonaDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<PersonaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PersonaDto> UpdateAsync(Guid id, UpdatePersonaRequest request, CancellationToken cancellationToken = default);
}
