using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfBotConfigurationRepository : IBotConfigurationRepository
{
    private readonly AiEmployeeDbContext _db;

    public EfBotConfigurationRepository(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public async Task<JudgeBotConfiguration> GetJudgeBotAsync()
    {
        var bot = await _db.Bots
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == JudgeBotDefaults.BotId);

        if (bot is null)
            throw new InvalidOperationException("JudgeBot not found in database");

        return await ResolveConfigurationForBotAsync(bot);
    }

    public async Task<JudgeBotConfiguration> GetByIntegrationAsync(string channel, string externalId)
    {
        if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(externalId))
            return await GetJudgeBotAsync();

        var normalizedChannel = channel.Trim().ToLowerInvariant();
        var trimmedExternalId = externalId.Trim();

        var integration = await _db.BotIntegrations
            .AsNoTracking()
            .FirstOrDefaultAsync(i =>
                i.Channel == normalizedChannel &&
                i.ExternalId == trimmedExternalId &&
                i.IsEnabled);

        if (integration is null)
            return await GetJudgeBotAsync();

        var bot = await _db.Bots
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == integration.BotId && b.IsEnabled);

        if (bot is null)
            return await GetJudgeBotAsync();

        return await ResolveConfigurationForBotAsync(bot);
    }

    private async Task<JudgeBotConfiguration> ResolveConfigurationForBotAsync(Bot bot)
    {
        var persona = await _db.Personas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == bot.PersonaId);

        var behavior = await _db.Behaviors
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bot.BehaviorId);

        var languageProfile = await _db.LanguageProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == bot.LanguageProfileId);

        var wrapperTemplate = await _db.PromptTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == JudgeBotDefaults.JudgeTranscriptWrapperTemplateName);

        if (wrapperTemplate is null)
            throw new InvalidOperationException("Wrapper template not found");

        return new JudgeBotConfiguration(
            bot,
            persona ?? throw new InvalidOperationException("Bot Persona not found in database."),
            behavior ?? throw new InvalidOperationException("Bot Behavior not found in database."),
            languageProfile ?? throw new InvalidOperationException("Bot LanguageProfile not found in database."),
            wrapperTemplate);
    }
}
