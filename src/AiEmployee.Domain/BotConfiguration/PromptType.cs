namespace AiEmployee.Domain.BotConfiguration;

/// <summary>
/// Archived prompt segments for <see cref="PromptVersion"/> (stored as integers in the database).
/// <see cref="Judge"/> / <see cref="Lead"/> are the full template strings on <see cref="PromptSections"/>.
/// Extension types mirror <see cref="Persona"/> optional instruction/schema columns (Phase 4).
/// </summary>
public enum PromptType
{
    System = 0,
    Judge = 1,
    Lead = 2,
    ChatOutputSchema = 3,
    JudgeInstruction = 4,
    JudgeSchema = 5,
    LeadInstruction = 6,
    LeadSchema = 7,
}
