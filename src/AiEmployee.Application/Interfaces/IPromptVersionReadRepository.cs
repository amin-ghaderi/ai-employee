using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Interfaces;

/// <summary>
/// Read-only access to <see cref="PromptVersion"/> rows for observability (e.g. max archived version per prompt type).
/// </summary>
public interface IPromptVersionReadRepository
{
    /// <summary>Returns the maximum <see cref="PromptVersion.Version"/> for the persona and type, or 0 when none exist.</summary>
    Task<int> GetMaxVersionAsync(Guid personaId, PromptType promptType, CancellationToken cancellationToken = default);
}
