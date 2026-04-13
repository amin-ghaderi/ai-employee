using System.Reflection;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Domain.Entities;
using AiEmployee.Domain.Settings;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiEmployee.DataMigrator.Migration;

public sealed class SqliteToPostgresMigrator
{
    private readonly ILogger<SqliteToPostgresMigrator> _logger;

    public SqliteToPostgresMigrator(ILogger<SqliteToPostgresMigrator> logger)
    {
        _logger = logger;
    }

    public async Task MigrateAsync(MigrationOptions options, CancellationToken cancellationToken = default)
    {
        var sqliteOptions = new DbContextOptionsBuilder<AiEmployeeDbContext>()
            .UseSqlite(options.SourceConnectionString, sqlite => sqlite.CommandTimeout(300))
            .Options;

        var postgresOptions = new DbContextOptionsBuilder<AiEmployeePostgresDbContext>()
            .UseNpgsql(options.TargetConnectionString, npgsql =>
            {
                npgsql.CommandTimeout(300);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Postgres", "public");
            })
            .Options;

        await using var source = new AiEmployeeDbContext(sqliteOptions);
        await using var target = new AiEmployeePostgresDbContext(postgresOptions);

        _logger.LogInformation("Source: SQLite | Target: PostgreSQL | BatchSize={Batch} | DryRun={DryRun} | Truncate={Truncate}",
            options.BatchSize,
            options.DryRun,
            options.TruncateBeforeImport);

        if (options.TruncateBeforeImport && !options.DryRun)
        {
            _logger.LogWarning("Truncating application tables on target (migration history table is preserved).");
            await TruncateTargetAsync(target, cancellationToken).ConfigureAwait(false);
        }
        else if (options.TruncateBeforeImport && options.DryRun)
        {
            _logger.LogInformation("[DryRun] Skipping truncate.");
        }

        if (!options.DryRun)
        {
            await using var tx = await target.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                target.ChangeTracker.AutoDetectChangesEnabled = false;
                await RunCopiesAsync(source, target, options, cancellationToken).ConfigureAwait(false);
                await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        else
        {
            await RunCopiesAsync(source, target, options, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Migration phase completed.");
    }

    private async Task RunCopiesAsync(
        AiEmployeeDbContext source,
        AiEmployeePostgresDbContext target,
        MigrationOptions options,
        CancellationToken cancellationToken)
    {
        await CopyOrderedAsync(
            source.Behaviors.AsNoTracking().OrderBy(e => e.Id),
            target.Behaviors,
            target,
            options,
            "Behaviors",
            cancellationToken).ConfigureAwait(false);

        await CopyOrderedAsync(
            source.LanguageProfiles.AsNoTracking().OrderBy(e => e.Id),
            target.LanguageProfiles,
            target,
            options,
            "LanguageProfiles",
            cancellationToken).ConfigureAwait(false);

        await CopyOrderedAsync(
            source.Personas.AsNoTracking().OrderBy(e => e.Id),
            target.Personas,
            target,
            options,
            "Personas",
            cancellationToken).ConfigureAwait(false);

        await CopyOrderedAsync(
            source.PromptTemplates.AsNoTracking().OrderBy(e => e.Id),
            target.PromptTemplates,
            target,
            options,
            "PromptTemplates",
            cancellationToken).ConfigureAwait(false);

        await CopyOrderedAsync(
            source.Users.AsNoTracking().OrderBy(e => e.Id),
            target.Users,
            target,
            options,
            "Users",
            cancellationToken).ConfigureAwait(false);

        await CopyOrderedAsync(
            source.Conversations.AsNoTracking().OrderBy(e => e.Id),
            target.Conversations,
            target,
            options,
            "Conversations",
            cancellationToken).ConfigureAwait(false);

        await CopyOrderedAsync(
            source.Messages.AsNoTracking().OrderBy(e => e.Id),
            target.Messages,
            target,
            options,
            "Messages",
            cancellationToken).ConfigureAwait(false);

        await CopyOrderedAsync(
            source.Bots.AsNoTracking().OrderBy(e => e.Id),
            target.Bots,
            target,
            options,
            "Bots",
            cancellationToken).ConfigureAwait(false);

        await CopyOrderedAsync(
            source.PromptVersions.AsNoTracking().OrderBy(e => e.Id),
            target.PromptVersions,
            target,
            options,
            "PromptVersions",
            cancellationToken).ConfigureAwait(false);

        await CopyOrderedAsync(
            source.Judgments.AsNoTracking().OrderBy(e => e.Id),
            target.Judgments,
            target,
            options,
            "Judgments",
            cancellationToken).ConfigureAwait(false);

        await CopyOrderedAsync(
            source.Leads.AsNoTracking().OrderBy(e => e.Id),
            target.Leads,
            target,
            options,
            "Leads",
            cancellationToken).ConfigureAwait(false);

        await CopyOrderedAsync(
            source.BotIntegrations.AsNoTracking().OrderBy(e => e.Id),
            target.BotIntegrations,
            target,
            options,
            "BotIntegrations",
            cancellationToken).ConfigureAwait(false);

        await CopyOrderedAsync(
            source.SystemSettings.AsNoTracking().OrderBy(e => e.Id),
            target.SystemSettings,
            target,
            options,
            "SystemSettings",
            cancellationToken).ConfigureAwait(false);

        await CopyOrderedAsync(
            source.ProcessedTelegramUpdates.AsNoTracking().OrderBy(e => e.BotScopeKey).ThenBy(e => e.TelegramUpdateId),
            target.ProcessedTelegramUpdates,
            target,
            options,
            "ProcessedTelegramUpdates",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task CopyOrderedAsync<TEntity>(
        IOrderedQueryable<TEntity> orderedSource,
        DbSet<TEntity> targetSet,
        AiEmployeePostgresDbContext target,
        MigrationOptions options,
        string tableName,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var skip = 0;
        var total = 0;
        while (true)
        {
            var batch = await orderedSource
                .Skip(skip)
                .Take(options.BatchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (batch.Count == 0)
                break;

            total += batch.Count;

            if (options.DryRun)
            {
                _logger.LogInformation("[DryRun] {Table}: batch rows={Count} (cumulative={Total})", tableName, batch.Count, total);
            }
            else
            {
                NormalizeTemporalValuesForNpgsql(batch);
                target.ChangeTracker.AutoDetectChangesEnabled = false;
                await targetSet.AddRangeAsync(batch, cancellationToken).ConfigureAwait(false);
                await target.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                target.ChangeTracker.Clear();
                _logger.LogInformation("{Table}: inserted batch rows={Count} (cumulative={Total})", tableName, batch.Count, total);
            }

            skip += batch.Count;
        }

        _logger.LogInformation("{Table}: finished. Total rows processed={Total}", tableName, total);
    }

    private static async Task TruncateTargetAsync(AiEmployeePostgresDbContext target, CancellationToken cancellationToken)
    {
        // Preserve __EFMigrationsHistory_Postgres. CASCADE clears dependent rows if FK graph requires it.
        await target.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                "BotIntegrations",
                "Leads",
                "Judgments",
                "PromptVersions",
                "Messages",
                "Bots",
                "SystemSettings",
                "ProcessedTelegramUpdates",
                "Conversations",
                "Users",
                "PromptTemplates",
                "Personas",
                "LanguageProfiles",
                "Behaviors"
            RESTART IDENTITY CASCADE;
            """,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// SQLite returns <see cref="DateTimeKind.Unspecified"/>; Npgsql requires UTC for <c>timestamptz</c>.
    /// </summary>
    private static void NormalizeTemporalValuesForNpgsql<TEntity>(List<TEntity> entities)
        where TEntity : class
    {
        if (entities.Count == 0)
            return;

        var type = typeof(TEntity);
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var setter = prop.GetSetMethod(true);
            if (setter is null)
                continue;

            var pt = prop.PropertyType;
            if (pt == typeof(DateTime))
            {
                foreach (var entity in entities)
                {
                    var v = (DateTime)prop.GetValue(entity)!;
                    prop.SetValue(entity, ToUtcDateTime(v));
                }
            }
            else if (pt == typeof(DateTime?))
            {
                foreach (var entity in entities)
                {
                    var v = (DateTime?)prop.GetValue(entity);
                    if (v.HasValue)
                        prop.SetValue(entity, ToUtcDateTime(v.Value));
                }
            }
            else if (pt == typeof(DateTimeOffset))
            {
                foreach (var entity in entities)
                {
                    var v = (DateTimeOffset)prop.GetValue(entity)!;
                    prop.SetValue(entity, v.ToUniversalTime());
                }
            }
            else if (pt == typeof(DateTimeOffset?))
            {
                foreach (var entity in entities)
                {
                    var v = (DateTimeOffset?)prop.GetValue(entity);
                    if (v.HasValue)
                        prop.SetValue(entity, v.Value.ToUniversalTime());
                }
            }
        }
    }

    private static DateTime ToUtcDateTime(DateTime v) => v.Kind switch
    {
        DateTimeKind.Utc => v,
        DateTimeKind.Local => v.ToUniversalTime(),
        _ => DateTime.SpecifyKind(v, DateTimeKind.Utc),
    };
}
