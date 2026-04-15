namespace AiEmployee.Application.Dtos.Personas;

public sealed class CreatePersonaRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public PromptSectionsDto Prompts { get; set; } = null!;
    public ClassificationSchemaDto ClassificationSchema { get; set; } = null!;

    /// <summary>Optional; when null, extension columns are unset.</summary>
    public PersonaPromptExtensionsDto? PromptExtensions { get; set; }
}
