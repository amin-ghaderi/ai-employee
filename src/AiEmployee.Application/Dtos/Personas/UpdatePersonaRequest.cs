namespace AiEmployee.Application.Dtos.Personas;

public sealed class UpdatePersonaRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public PromptSectionsDto Prompts { get; set; } = null!;
    public ClassificationSchemaDto ClassificationSchema { get; set; } = null!;

    /// <summary>
    /// When null, existing extension fields in the database are preserved.
    /// When set, all extension fields are replaced with these values (use empty strings to clear).
    /// </summary>
    public PersonaPromptExtensionsDto? PromptExtensions { get; set; }
}
