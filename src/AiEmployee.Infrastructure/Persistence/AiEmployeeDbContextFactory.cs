using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiEmployee.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core CLI (migrations) without starting the web host.
/// Set <c>Database__Provider</c> and <c>ConnectionStrings__DefaultConnection</c> for PostgreSQL.
/// </summary>
public sealed class AiEmployeeDbContextFactory : IDesignTimeDbContextFactory<AiEmployeeDbContext>
{
    public AiEmployeeDbContext CreateDbContext(string[] args)
    {
        var provider = Environment.GetEnvironmentVariable("Database__Provider") ?? "Sqlite";

        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? (IsPostgresProvider(provider)
                ? "Host=localhost;Port=5432;Database=aiemployee;Username=postgres;Password=postgres"
                : "Data Source=aiemployee.db");

        var optionsBuilder = new DbContextOptionsBuilder<AiEmployeeDbContext>();

        if (IsPostgresProvider(provider))
        {
            optionsBuilder.UseNpgsql(connectionString);
        }
        else
        {
            optionsBuilder.UseSqlite(connectionString);
        }

        return new AiEmployeeDbContext(optionsBuilder.Options);
    }

    private static bool IsPostgresProvider(string provider) =>
        provider.Equals("Npgsql", StringComparison.OrdinalIgnoreCase)
        || provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase);
}
