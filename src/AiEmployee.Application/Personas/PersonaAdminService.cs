using AiEmployee.Application.Dtos.Personas;
using AiEmployee.Application.Interfaces;

namespace AiEmployee.Application.Personas;

public sealed class PersonaAdminService : IPersonaAdminService
{
    private readonly IPersonaRepository _repository;

    public PersonaAdminService(IPersonaRepository repository)
    {
        _repository = repository;
    }

    public async Task<PersonaDto> CreateAsync(
        CreatePersonaRequest request,
        CancellationToken cancellationToken = default)
    {
        PersonaRequestValidator.Validate(request);
        var id = Guid.NewGuid();
        var persona = PersonaMapper.ToDomain(id, request);
        await _repository.AddAsync(persona, cancellationToken).ConfigureAwait(false);
        return PersonaMapper.ToDto(persona);
    }

    public async Task<IReadOnlyList<PersonaDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return list.Select(PersonaMapper.ToDto).ToList();
    }

    public async Task<PersonaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var persona = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return persona is null ? null : PersonaMapper.ToDto(persona);
    }

    public async Task<PersonaDto> UpdateAsync(
        Guid id,
        UpdatePersonaRequest request,
        CancellationToken cancellationToken = default)
    {
        PersonaRequestValidator.Validate(request);
        var existing = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No persona was found for id '{id}'.");

        var extensionsUpdate = request.PromptExtensions is null
            ? null
            : new PersonaPromptExtensionsUpdate(
                Apply: true,
                request.PromptExtensions.ChatOutputSchemaJson,
                request.PromptExtensions.JudgeInstruction,
                request.PromptExtensions.JudgeSchemaJson,
                request.PromptExtensions.LeadInstruction,
                request.PromptExtensions.LeadSchemaJson);

        await _repository.UpdateAsync(
            id,
            request.DisplayName,
            request.Prompts.System,
            request.Prompts.Judge,
            request.Prompts.Lead,
            request.ClassificationSchema.UserTypes ?? Array.Empty<string>(),
            request.ClassificationSchema.Intents ?? Array.Empty<string>(),
            request.ClassificationSchema.Potentials ?? Array.Empty<string>(),
            extensionsUpdate,
            cancellationToken).ConfigureAwait(false);

        var updated = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"No persona was found for id '{id}'.");
        return PersonaMapper.ToDto(updated);
    }
}
