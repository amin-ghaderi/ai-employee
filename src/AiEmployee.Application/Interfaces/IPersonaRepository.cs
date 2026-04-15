using AiEmployee.Application.Dtos.Personas;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Interfaces;

public interface IPersonaRepository
{
    Task<Persona?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Persona>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Persona persona, CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        string displayName,
        string systemPrompt,
        string judgePrompt,
        string leadPrompt,
        IReadOnlyList<string> userTypes,
        IReadOnlyList<string> intents,
        IReadOnlyList<string> potentials,
        PersonaPromptExtensionsUpdate? promptExtensions = null,
        CancellationToken cancellationToken = default);
}
