using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Personas;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfBotConfigurationCommand : IBotConfigurationCommand
{
    private readonly AiEmployeeDbContext _db;

    public EfBotConfigurationCommand(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public async Task UpdatePromptsAsync(
        Guid botId,
        string judgePrompt,
        string leadPrompt,
        CancellationToken cancellationToken = default)
    {
        var bot = await _db.Bots
            .FirstOrDefaultAsync(b => b.Id == botId, cancellationToken)
            .ConfigureAwait(false);

        if (bot is null)
            throw new KeyNotFoundException($"No bot was found for id '{botId}'.");

        var persona = await _db.Personas
            .FirstOrDefaultAsync(p => p.Id == bot.PersonaId, cancellationToken)
            .ConfigureAwait(false);

        if (persona is null)
            throw new KeyNotFoundException($"No persona was found for bot '{botId}'.");

        PersonaRequestValidator.ValidateJudgeAndLeadForBotConfigUpdate(judgePrompt, leadPrompt);

        var oldJudge = persona.Prompts.Judge;
        var oldLead = persona.Prompts.Lead;

        await PromptVersionRecorder.RecordJudgeAndLeadIfChangedAsync(
                _db,
                persona.Id,
                oldJudge,
                judgePrompt,
                oldLead,
                leadPrompt,
                cancellationToken)
            .ConfigureAwait(false);

        persona.UpdateJudgeAndLeadPrompts(judgePrompt, leadPrompt);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
