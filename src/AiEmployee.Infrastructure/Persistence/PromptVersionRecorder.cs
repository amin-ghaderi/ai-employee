using AiEmployee.Domain.BotConfiguration;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Persistence;

/// <summary>
/// Appends a <see cref="PromptVersion"/> row when a persona prompt field changes (stores the previous text).
/// </summary>
internal static class PromptVersionRecorder
{
    private const string CreatedBySystem = "system";

    public static async Task AppendIfChangedAsync(
        AiEmployeeDbContext db,
        Guid personaId,
        PromptType promptType,
        string? oldContent,
        string? newContent,
        CancellationToken cancellationToken)
    {
        var oldNorm = oldContent ?? string.Empty;
        var newNorm = newContent ?? string.Empty;
        if (string.Equals(oldNorm, newNorm, StringComparison.Ordinal))
            return;

        var maxVersion = await db.PromptVersions
            .AsNoTracking()
            .Where(v => v.PersonaId == personaId && v.PromptType == promptType)
            .Select(v => (int?)v.Version)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false) ?? 0;

        db.PromptVersions.Add(new PromptVersion(
            Guid.NewGuid(),
            personaId,
            promptType,
            maxVersion + 1,
            oldNorm,
            DateTime.UtcNow,
            CreatedBySystem));
    }

    public static async Task RecordSystemJudgeLeadIfChangedAsync(
        AiEmployeeDbContext db,
        Guid personaId,
        string oldSystem,
        string newSystem,
        string oldJudge,
        string newJudge,
        string oldLead,
        string newLead,
        CancellationToken cancellationToken)
    {
        await AppendIfChangedAsync(db, personaId, PromptType.System, oldSystem, newSystem, cancellationToken)
            .ConfigureAwait(false);
        await AppendIfChangedAsync(db, personaId, PromptType.Judge, oldJudge, newJudge, cancellationToken)
            .ConfigureAwait(false);
        await AppendIfChangedAsync(db, personaId, PromptType.Lead, oldLead, newLead, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task RecordJudgeAndLeadIfChangedAsync(
        AiEmployeeDbContext db,
        Guid personaId,
        string oldJudge,
        string newJudge,
        string oldLead,
        string newLead,
        CancellationToken cancellationToken)
    {
        await AppendIfChangedAsync(db, personaId, PromptType.Judge, oldJudge, newJudge, cancellationToken)
            .ConfigureAwait(false);
        await AppendIfChangedAsync(db, personaId, PromptType.Lead, oldLead, newLead, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Records prior values for <see cref="Persona"/> extension columns when they change (dual-read rollout; Behavior columns unchanged).
    /// </summary>
    public static async Task RecordPersonaPromptExtensionsIfChangedAsync(
        AiEmployeeDbContext db,
        Guid personaId,
        string? oldChatOutputSchemaJson,
        string? newChatOutputSchemaJson,
        string? oldJudgeInstruction,
        string? newJudgeInstruction,
        string? oldJudgeSchemaJson,
        string? newJudgeSchemaJson,
        string? oldLeadInstruction,
        string? newLeadInstruction,
        string? oldLeadSchemaJson,
        string? newLeadSchemaJson,
        CancellationToken cancellationToken)
    {
        await AppendIfChangedAsync(
                db,
                personaId,
                PromptType.ChatOutputSchema,
                oldChatOutputSchemaJson,
                newChatOutputSchemaJson,
                cancellationToken)
            .ConfigureAwait(false);
        await AppendIfChangedAsync(
                db,
                personaId,
                PromptType.JudgeInstruction,
                oldJudgeInstruction,
                newJudgeInstruction,
                cancellationToken)
            .ConfigureAwait(false);
        await AppendIfChangedAsync(
                db,
                personaId,
                PromptType.JudgeSchema,
                oldJudgeSchemaJson,
                newJudgeSchemaJson,
                cancellationToken)
            .ConfigureAwait(false);
        await AppendIfChangedAsync(
                db,
                personaId,
                PromptType.LeadInstruction,
                oldLeadInstruction,
                newLeadInstruction,
                cancellationToken)
            .ConfigureAwait(false);
        await AppendIfChangedAsync(
                db,
                personaId,
                PromptType.LeadSchema,
                oldLeadSchemaJson,
                newLeadSchemaJson,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
