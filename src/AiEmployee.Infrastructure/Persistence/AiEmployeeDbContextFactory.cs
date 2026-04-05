using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiEmployee.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core CLI (migrations) without starting the web host.
/// </summary>
public sealed class AiEmployeeDbContextFactory : IDesignTimeDbContextFactory<AiEmployeeDbContext>
{
    public AiEmployeeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AiEmployeeDbContext>()
            .UseSqlite("Data Source=aiemployee.db");

        return new AiEmployeeDbContext(optionsBuilder.Options);
    }
}
