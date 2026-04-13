using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiEmployee.DataMigrator.Validation;

public sealed class RowCountValidator
{
    private readonly ILogger<RowCountValidator> _logger;

    public RowCountValidator(ILogger<RowCountValidator> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(
        string sqliteConnectionString,
        string postgresConnectionString,
        CancellationToken cancellationToken = default)
    {
        var sqliteOptions = new DbContextOptionsBuilder<AiEmployeeDbContext>()
            .UseSqlite(sqliteConnectionString)
            .Options;

        var postgresOptions = new DbContextOptionsBuilder<AiEmployeePostgresDbContext>()
            .UseNpgsql(postgresConnectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Postgres", "public"))
            .Options;

        await using var source = new AiEmployeeDbContext(sqliteOptions);
        await using var target = new AiEmployeePostgresDbContext(postgresOptions);

        var pairs = new (string Name, Func<Task<int>> Source, Func<Task<int>> Target)[]
        {
            ("Behaviors", () => source.Behaviors.CountAsync(cancellationToken), () => target.Behaviors.CountAsync(cancellationToken)),
            ("LanguageProfiles", () => source.LanguageProfiles.CountAsync(cancellationToken), () => target.LanguageProfiles.CountAsync(cancellationToken)),
            ("Personas", () => source.Personas.CountAsync(cancellationToken), () => target.Personas.CountAsync(cancellationToken)),
            ("PromptTemplates", () => source.PromptTemplates.CountAsync(cancellationToken), () => target.PromptTemplates.CountAsync(cancellationToken)),
            ("Users", () => source.Users.CountAsync(cancellationToken), () => target.Users.CountAsync(cancellationToken)),
            ("Conversations", () => source.Conversations.CountAsync(cancellationToken), () => target.Conversations.CountAsync(cancellationToken)),
            ("Messages", () => source.Messages.CountAsync(cancellationToken), () => target.Messages.CountAsync(cancellationToken)),
            ("Bots", () => source.Bots.CountAsync(cancellationToken), () => target.Bots.CountAsync(cancellationToken)),
            ("PromptVersions", () => source.PromptVersions.CountAsync(cancellationToken), () => target.PromptVersions.CountAsync(cancellationToken)),
            ("Judgments", () => source.Judgments.CountAsync(cancellationToken), () => target.Judgments.CountAsync(cancellationToken)),
            ("Leads", () => source.Leads.CountAsync(cancellationToken), () => target.Leads.CountAsync(cancellationToken)),
            ("BotIntegrations", () => source.BotIntegrations.CountAsync(cancellationToken), () => target.BotIntegrations.CountAsync(cancellationToken)),
            ("SystemSettings", () => source.SystemSettings.CountAsync(cancellationToken), () => target.SystemSettings.CountAsync(cancellationToken)),
            ("ProcessedTelegramUpdates", () => source.ProcessedTelegramUpdates.CountAsync(cancellationToken), () => target.ProcessedTelegramUpdates.CountAsync(cancellationToken)),
        };

        var ok = true;
        foreach (var (name, srcFn, tgtFn) in pairs)
        {
            var s = await srcFn().ConfigureAwait(false);
            var t = await tgtFn().ConfigureAwait(false);
            var match = s == t;
            if (!match)
                ok = false;

            _logger.LogInformation(
                "RowCount [{Table}] Source={SourceCount} Target={TargetCount} {Status}",
                name,
                s,
                t,
                match ? "OK" : "MISMATCH");
        }

        return ok;
    }
}
