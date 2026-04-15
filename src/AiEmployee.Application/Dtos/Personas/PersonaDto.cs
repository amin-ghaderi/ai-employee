namespace AiEmployee.Application.Dtos.Personas;

public sealed class PersonaDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public PromptSectionsDto Prompts { get; set; } = null!;
    public ClassificationSchemaDto ClassificationSchema { get; set; } = null!;

    /// <summary>Chat output schema plus judge/lead instruction overrides (canonical on Persona; Behavior remains legacy fallback at runtime until deprecated).</summary>
    public PersonaPromptExtensionsDto PromptExtensions { get; set; } = new();
}
