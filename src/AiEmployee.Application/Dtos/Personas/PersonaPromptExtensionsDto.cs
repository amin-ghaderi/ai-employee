namespace AiEmployee.Application.Dtos.Personas;

/// <summary>Optional judge/lead instruction + schema and chat output schema (Persona aggregate extensions).</summary>
public sealed class PersonaPromptExtensionsDto
{
    public string? ChatOutputSchemaJson { get; set; }

    public string? JudgeInstruction { get; set; }

    public string? JudgeSchemaJson { get; set; }

    public string? LeadInstruction { get; set; }

    public string? LeadSchemaJson { get; set; }
}
