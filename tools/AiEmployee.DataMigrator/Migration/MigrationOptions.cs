namespace AiEmployee.DataMigrator.Migration;

public sealed class MigrationOptions
{
    public string SourceConnectionString { get; init; } = string.Empty;

    public string TargetConnectionString { get; init; } = string.Empty;

    public int BatchSize { get; init; } = 500;

    public bool DryRun { get; init; }

    public bool TruncateBeforeImport { get; init; }

    public bool ValidateOnly { get; init; }
}
