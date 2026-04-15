using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace AiEmployee.IntegrationTests;

/// <summary>
/// Spins up a disposable PostgreSQL instance (pgvector image) and points the API at it.
/// </summary>
public class PostgresWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .WithDatabase("aiemployee_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Host settings override appsettings.json (in-memory config does not).
        builder.UseSetting("ConnectionStrings:DefaultConnection", _container.GetConnectionString());
        ConfigureWebHostExtras(builder);
    }

    /// <summary>Derived fixtures add Slack/WhatsApp keys and other overrides.</summary>
    protected virtual void ConfigureWebHostExtras(IWebHostBuilder builder)
    {
    }

    public async Task InitializeAsync() => await _container.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);
        await _container.DisposeAsync().ConfigureAwait(false);
    }
}
