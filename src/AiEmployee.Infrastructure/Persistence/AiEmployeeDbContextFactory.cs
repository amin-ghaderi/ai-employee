using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Persistence;

/// <summary>Design-time factory for EF Core CLI (migrations) without starting the web host.</summary>
public sealed class AiEmployeeDbContextFactory : IDesignTimeDbContextFactory<AiEmployeeDbContext>
{
    public AiEmployeeDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("POSTGRES_DESIGN_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=aiemployee;Username=aiemployee;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AiEmployeeDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Postgres", "public");
            npgsql.CommandTimeout(60);
            npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), null);
            npgsql.UseVector();
        });

        return new AiEmployeeDbContext(optionsBuilder.Options);
    }
}
