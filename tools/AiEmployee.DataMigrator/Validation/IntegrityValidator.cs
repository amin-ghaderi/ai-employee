using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiEmployee.DataMigrator.Validation;

public sealed class IntegrityValidator
{
    private readonly ILogger<IntegrityValidator> _logger;

    public IntegrityValidator(ILogger<IntegrityValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Verifies foreign key consistency on the PostgreSQL target only.
    /// </summary>
    public async Task<bool> ValidateTargetAsync(string postgresConnectionString, CancellationToken cancellationToken = default)
    {
        var postgresOptions = new DbContextOptionsBuilder<AiEmployeePostgresDbContext>()
            .UseNpgsql(postgresConnectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Postgres", "public"))
            .Options;

        await using var target = new AiEmployeePostgresDbContext(postgresOptions);

        var ok = true;

        var orphanMessages = await target.Messages
            .Where(m => !target.Conversations.Any(c => c.Id == m.ConversationId))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        LogFk("Messages -> Conversations", orphanMessages, ref ok);

        var orphanBotsBehavior = await target.Bots
            .Where(b => !target.Behaviors.Any(x => x.Id == b.BehaviorId))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        LogFk("Bots -> Behaviors", orphanBotsBehavior, ref ok);

        var orphanBotsPersona = await target.Bots
            .Where(b => !target.Personas.Any(x => x.Id == b.PersonaId))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        LogFk("Bots -> Personas", orphanBotsPersona, ref ok);

        var orphanBotsLang = await target.Bots
            .Where(b => !target.LanguageProfiles.Any(x => x.Id == b.LanguageProfileId))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        LogFk("Bots -> LanguageProfiles", orphanBotsLang, ref ok);

        var orphanPromptVersions = await target.PromptVersions
            .Where(p => !target.Personas.Any(x => x.Id == p.PersonaId))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        LogFk("PromptVersions -> Personas", orphanPromptVersions, ref ok);

        var orphanJudgmentsConv = await target.Judgments
            .Where(j => !target.Conversations.Any(c => c.Id == j.ConversationId))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        LogFk("Judgments -> Conversations", orphanJudgmentsConv, ref ok);

        var orphanJudgmentsUser = await target.Judgments
            .Where(j => !target.Users.Any(u => u.Id == j.UserId))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        LogFk("Judgments -> Users", orphanJudgmentsUser, ref ok);

        var orphanLeads = await target.Leads
            .Where(l => !target.Users.Any(u => u.Id == l.UserId))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        LogFk("Leads -> Users", orphanLeads, ref ok);

        var orphanBotIntegrations = await target.BotIntegrations
            .Where(i => !target.Bots.Any(b => b.Id == i.BotId))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        LogFk("BotIntegrations -> Bots", orphanBotIntegrations, ref ok);

        return ok;
    }

    private void LogFk(string relationship, int orphanCount, ref bool ok)
    {
        if (orphanCount > 0)
            ok = false;

        _logger.LogInformation(
            "FK [{Relationship}] orphan rows={Count} {Status}",
            relationship,
            orphanCount,
            orphanCount == 0 ? "OK" : "FAILED");
    }
}
