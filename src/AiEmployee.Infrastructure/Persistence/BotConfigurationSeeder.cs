using AiEmployee.Application.Prompting;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.Persistence;

public sealed class BotConfigurationSeeder
{
    private readonly AiEmployeeDbContext _db;
    private readonly IOptions<TelegramSettings> _telegramSettings;

    public BotConfigurationSeeder(AiEmployeeDbContext db, IOptions<TelegramSettings> telegramSettings)
    {
        _db = db;
        _telegramSettings = telegramSettings;
    }

    /// <summary>
    /// Inserts JudgeBot default configuration if not already present.
    /// Idempotent: core rows inserted only when missing. Telegram <see cref="BotIntegration"/> is ensured when <see cref="TelegramSettings.BotToken"/> is set.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await _db.Bots.AnyAsync(b => b.Id == JudgeBotDefaults.BotId, cancellationToken))
        {
            var wrapperTemplate = JudgeBotDefaults.CreateJudgeTranscriptWrapperTemplate();
            PromptTokens.ThrowIfJudgeWrapperMissingTranscriptPlaceholder(wrapperTemplate.Template);

            if (!await _db.Personas.AnyAsync(p => p.Id == JudgeBotDefaults.PersonaId, cancellationToken))
                _db.Personas.Add(JudgeBotDefaults.CreatePersona());

            if (!await _db.Behaviors.AnyAsync(b => b.Id == JudgeBotDefaults.BehaviorId, cancellationToken))
                _db.Behaviors.Add(JudgeBotDefaults.CreateBehavior());

            if (!await _db.LanguageProfiles.AnyAsync(l => l.Id == JudgeBotDefaults.LanguageProfileId, cancellationToken))
                _db.LanguageProfiles.Add(JudgeBotDefaults.CreateLanguageProfile());

            if (!await _db.PromptTemplates.AnyAsync(
                    t => t.Id == wrapperTemplate.Id || t.Name == wrapperTemplate.Name,
                    cancellationToken))
                _db.PromptTemplates.Add(wrapperTemplate);

            _db.Bots.Add(JudgeBotDefaults.CreateBot());

            await _db.SaveChangesAsync(cancellationToken);
        }

        await EnsureTelegramIntegrationAsync(cancellationToken);
        await RefreshWrapperTemplateAsync(cancellationToken);
        await EnsureDefaultBehaviorFlagsAsync(cancellationToken);
    }

    private async Task EnsureDefaultBehaviorFlagsAsync(CancellationToken cancellationToken)
    {
        var id = JudgeBotDefaults.BehaviorId;
        var behavior = await _db.Behaviors
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (behavior is null)
            return;

        if (!behavior.EnableJudge || !behavior.EnableChat || !behavior.EnableLead)
        {
            await _db.Behaviors
                .Where(b => b.Id == id && (!b.EnableJudge || !b.EnableChat || !b.EnableLead))
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(b => b.EnableJudge, true)
                        .SetProperty(b => b.EnableChat, true)
                        .SetProperty(b => b.EnableLead, true),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task RefreshWrapperTemplateAsync(CancellationToken cancellationToken)
    {
        var expected = JudgeBotDefaults.CreateJudgeTranscriptWrapperTemplate();
        PromptTokens.ThrowIfJudgeWrapperMissingTranscriptPlaceholder(expected.Template);

        var current = await _db.PromptTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == expected.Id, cancellationToken)
            .ConfigureAwait(false);

        if (current is not null && current.Template != expected.Template)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE PromptTemplates SET Template = {expected.Template} WHERE Id = {expected.Id}",
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EnsureTelegramIntegrationAsync(CancellationToken cancellationToken)
    {
        var token = _telegramSettings.Value.BotToken?.Trim();
        if (string.IsNullOrEmpty(token))
            return;

        if (!await _db.Bots.AnyAsync(b => b.Id == JudgeBotDefaults.BotId && b.IsEnabled, cancellationToken))
            return;

        var channel = BotIntegrationChannelNames.Telegram;
        if (await _db.BotIntegrations.AnyAsync(
                i => i.Channel == channel && i.ExternalId == token,
                cancellationToken))
            return;

        _db.BotIntegrations.Add(new BotIntegration(
            Guid.NewGuid(),
            JudgeBotDefaults.BotId,
            channel,
            token,
            isEnabled: true));

        await _db.SaveChangesAsync(cancellationToken);
    }
}

