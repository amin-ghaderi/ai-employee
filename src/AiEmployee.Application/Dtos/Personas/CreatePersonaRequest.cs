namespace AiEmployee.Application.Dtos.Personas;

public sealed class CreatePersonaRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public PromptSectionsDto Prompts { get; set; } = null!;
    public ClassificationSchemaDto ClassificationSchema { get; set; } = null!;
}
