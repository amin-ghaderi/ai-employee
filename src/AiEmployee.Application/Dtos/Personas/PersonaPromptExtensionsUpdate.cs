namespace AiEmployee.Application.Dtos.Personas;

/// <summary>
/// When <see cref="Apply"/> is true, repository replaces all extension columns with the given values (null clears after normalization).
/// When false, extension columns are left unchanged (backward compatible for clients that omit <c>promptExtensions</c>).
/// </summary>
public sealed record PersonaPromptExtensionsUpdate(
    bool Apply,
    string? ChatOutputSchemaJson = null,
    string? JudgeInstruction = null,
    string? JudgeSchemaJson = null,
    string? LeadInstruction = null,
    string? LeadSchemaJson = null);
