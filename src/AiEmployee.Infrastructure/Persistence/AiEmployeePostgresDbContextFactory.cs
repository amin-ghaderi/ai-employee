using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so EF migrations can build the PostgreSQL model (requires <c>UseVector()</c>).
/// </summary>
public sealed class AiEmployeePostgresDbContextFactory : IDesignTimeDbContextFactory<AiEmployeePostgresDbContext>
{
    public AiEmployeePostgresDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("POSTGRES_DESIGN_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=aiemployee;Username=aiemployee;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AiEmployeePostgresDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Postgres", "public");
            npgsql.UseVector();
        });

        return new AiEmployeePostgresDbContext(optionsBuilder.Options);
    }
}
