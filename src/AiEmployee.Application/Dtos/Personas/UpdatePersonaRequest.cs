namespace AiEmployee.Application.Dtos.Personas;

public sealed class UpdatePersonaRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public PromptSectionsDto Prompts { get; set; } = null!;
    public ClassificationSchemaDto ClassificationSchema { get; set; } = null!;
}
