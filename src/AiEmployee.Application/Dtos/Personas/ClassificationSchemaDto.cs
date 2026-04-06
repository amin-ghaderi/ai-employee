namespace AiEmployee.Application.Dtos.Personas;

public sealed class ClassificationSchemaDto
{
    public IReadOnlyList<string> UserTypes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Intents { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Potentials { get; set; } = Array.Empty<string>();
}
