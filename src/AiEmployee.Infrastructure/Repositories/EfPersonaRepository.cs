using AiEmployee.Application.Dtos.Personas;
using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfPersonaRepository : IPersonaRepository
{
    private readonly AiEmployeeDbContext _db;

    public EfPersonaRepository(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public async Task<Persona?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Personas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Persona>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Personas
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return list;
    }

    public async Task AddAsync(Persona persona, CancellationToken cancellationToken = default)
    {
        _db.Personas.Add(persona);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(
        Guid id,
        string displayName,
        string systemPrompt,
        string judgePrompt,
        string leadPrompt,
        IReadOnlyList<string> userTypes,
        IReadOnlyList<string> intents,
        IReadOnlyList<string> potentials,
        PersonaPromptExtensionsUpdate? promptExtensions = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.Personas
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No persona was found for id '{id}'.");

        var oldSystem = existing.Prompts.System;
        var oldJudge = existing.Prompts.Judge;
        var oldLead = existing.Prompts.Lead;

        await PromptVersionRecorder.RecordSystemJudgeLeadIfChangedAsync(
                _db,
                id,
                oldSystem,
                systemPrompt,
                oldJudge,
                judgePrompt,
                oldLead,
                leadPrompt,
                cancellationToken)
            .ConfigureAwait(false);

        existing.UpdateAll(
            displayName,
            systemPrompt,
            judgePrompt,
            leadPrompt,
            new ClassificationSchema(
                userTypes ?? Array.Empty<string>(),
                intents ?? Array.Empty<string>(),
                potentials ?? Array.Empty<string>()));

        if (promptExtensions is { Apply: true })
        {
            await PromptVersionRecorder.RecordPersonaPromptExtensionsIfChangedAsync(
                    _db,
                    id,
                    existing.ChatOutputSchemaJson,
                    promptExtensions.ChatOutputSchemaJson,
                    existing.JudgeInstruction,
                    promptExtensions.JudgeInstruction,
                    existing.JudgeSchemaJson,
                    promptExtensions.JudgeSchemaJson,
                    existing.LeadInstruction,
                    promptExtensions.LeadInstruction,
                    existing.LeadSchemaJson,
                    promptExtensions.LeadSchemaJson,
                    cancellationToken)
                .ConfigureAwait(false);

            existing.ReplacePromptExtensionFields(
                promptExtensions.ChatOutputSchemaJson,
                promptExtensions.JudgeInstruction,
                promptExtensions.JudgeSchemaJson,
                promptExtensions.LeadInstruction,
                promptExtensions.LeadSchemaJson);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
