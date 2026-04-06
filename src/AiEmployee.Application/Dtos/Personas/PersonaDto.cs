namespace AiEmployee.Application.Dtos.Personas;

public sealed class PersonaDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public PromptSectionsDto Prompts { get; set; } = null!;
    public ClassificationSchemaDto ClassificationSchema { get; set; } = null!;
}
