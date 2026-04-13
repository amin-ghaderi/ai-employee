using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiEmployee.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for PostgreSQL migrations (<see cref="AiEmployeePostgresDbContext"/>).
/// Set <c>ConnectionStrings__DefaultConnection</c> (and optionally <c>Database__Provider</c>=Npgsql for tooling scripts).
/// </summary>
public sealed class AiEmployeePostgresDbContextFactory : IDesignTimeDbContextFactory<AiEmployeePostgresDbContext>
{
    public AiEmployeePostgresDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=aiemployee;Username=aiemployee;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AiEmployeePostgresDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Postgres", "public");
                npgsql.CommandTimeout(60);
            });

        return new AiEmployeePostgresDbContext(optionsBuilder.Options);
    }
}
