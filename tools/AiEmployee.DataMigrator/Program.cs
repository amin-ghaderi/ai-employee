using AiEmployee.DataMigrator.Migration;
using AiEmployee.DataMigrator.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiEmployee.DataMigrator;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "MIGRATOR__")
            .AddCommandLine(args)
            .Build();

        using var provider = BuildServiceProvider(configuration);

        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("AiEmployee.DataMigrator");

        try
        {
            var options = ParseOptions(args, configuration, logger);
            if (options is null)
            {
                PrintUsage(logger);
                return 1;
            }

            if (options.ValidateOnly)
            {
                logger.LogInformation("Running validation only (no data copy).");
                return await RunValidationAsync(provider, options, logger).ConfigureAwait(false) ? 0 : 2;
            }

            var migrator = provider.GetRequiredService<SqliteToPostgresMigrator>();
            await migrator.MigrateAsync(options).ConfigureAwait(false);

            if (!options.DryRun)
            {
                logger.LogInformation("Running post-migration validation.");
                var valid = await RunValidationAsync(provider, options, logger).ConfigureAwait(false);
                return valid ? 0 : 2;
            }

            logger.LogInformation("Dry run complete; skipping post-migration validation.");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration failed.");
            return 1;
        }
    }

    private static ServiceProvider BuildServiceProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging(b =>
        {
            b.AddConfiguration(configuration.GetSection("Logging"));
            b.AddConsole();
        });
        services.AddSingleton<SqliteToPostgresMigrator>();
        services.AddSingleton<RowCountValidator>();
        services.AddSingleton<IntegrityValidator>();
        return services.BuildServiceProvider();
    }

    private static MigrationOptions? ParseOptions(string[] args, IConfiguration configuration, ILogger logger)
    {
        var source = configuration["source"] ?? configuration["Source"];
        var target = configuration["target"] ?? configuration["Target"];
        var batchSize = configuration.GetValue("batch-size", 500);
        var dryRun = configuration.GetValue("dry-run", false) || args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
        var truncate = configuration.GetValue("truncate", false) || args.Contains("--truncate", StringComparer.OrdinalIgnoreCase);
        var validateOnly = args.Contains("--validate-only", StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--source" when i + 1 < args.Length:
                    source = args[++i];
                    break;
                case "--target" when i + 1 < args.Length:
                    target = args[++i];
                    break;
                case "--batch-size" when i + 1 < args.Length && int.TryParse(args[++i], out var bs):
                    batchSize = bs;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--truncate":
                    truncate = true;
                    break;
                case "--validate-only":
                    validateOnly = true;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            logger.LogError("Missing --source or --target (or MIGRATOR__Source / MIGRATOR__Target).");
            return null;
        }

        if (batchSize < 1 || batchSize > 50_000)
        {
            logger.LogError("Invalid --batch-size (use 1..50000).");
            return null;
        }

        return new MigrationOptions
        {
            SourceConnectionString = source.Trim(),
            TargetConnectionString = target.Trim(),
            BatchSize = batchSize,
            DryRun = dryRun,
            TruncateBeforeImport = truncate,
            ValidateOnly = validateOnly,
        };
    }

    private static async Task<bool> RunValidationAsync(
        IServiceProvider provider,
        MigrationOptions options,
        ILogger logger)
    {
        var rowValidator = provider.GetRequiredService<RowCountValidator>();
        var intValidator = provider.GetRequiredService<IntegrityValidator>();

        var countsOk = await rowValidator
            .ValidateAsync(options.SourceConnectionString, options.TargetConnectionString)
            .ConfigureAwait(false);
        var fkOk = await intValidator
            .ValidateTargetAsync(options.TargetConnectionString)
            .ConfigureAwait(false);

        var ok = countsOk && fkOk;
        logger.LogInformation("Validation result: {Result}", ok ? "PASSED" : "FAILED");
        return ok;
    }

    private static void PrintUsage(ILogger logger)
    {
        logger.LogInformation(
            """
            AiEmployee.DataMigrator — SQLite → PostgreSQL ETL

            Usage:
              AiEmployee.DataMigrator --source "<sqlite cs>" --target "<npgsql cs>" [options]

            Options:
              --batch-size <n>     Rows per batch (default 500)
              --dry-run            Log batches without writing to PostgreSQL
              --truncate           TRUNCATE application tables on target before import (destructive)
              --validate-only      Compare row counts + FK integrity; no copy

            Environment (optional, prefix MIGRATOR__):
              MIGRATOR__Source, MIGRATOR__Target, MIGRATOR__batch-size

            Example:
              dotnet run --project tools/AiEmployee.DataMigrator -- ^
                --source "Data Source=./aiemployee.db" ^
                --target "Host=localhost;Port=5432;Database=aiemployee;Username=aiemployee;Password=postgres" ^
                --batch-size 500 --truncate
            """);
    }
}
