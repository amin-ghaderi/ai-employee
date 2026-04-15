using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Domain.Entities;
using AiEmployee.Domain.Settings;
using AiEmployee.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Persistence;

/// <summary>Canonical EF Core context for PostgreSQL (runtime, migrations, and design-time).</summary>
public sealed class AiEmployeeDbContext : DbContext
{
    public AiEmployeeDbContext(DbContextOptions<AiEmployeeDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Judgment> Judgments => Set<Judgment>();

    public DbSet<Bot> Bots => Set<Bot>();
    public DbSet<BotIntegration> BotIntegrations => Set<BotIntegration>();
    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<Behavior> Behaviors => Set<Behavior>();
    public DbSet<LanguageProfile> LanguageProfiles => Set<LanguageProfile>();
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
    public DbSet<PromptVersion> PromptVersions => Set<PromptVersion>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<ProcessedTelegramUpdate> ProcessedTelegramUpdates => Set<ProcessedTelegramUpdate>();

    public DbSet<MessageEmbeddingEntity> MessageEmbeddings => Set<MessageEmbeddingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AiEmployeeDbContext).Assembly);
    }
}
