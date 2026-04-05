using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Persistence;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AiEmployeeDbContext).Assembly);
    }
}
