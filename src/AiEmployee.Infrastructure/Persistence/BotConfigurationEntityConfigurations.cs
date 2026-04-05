using AiEmployee.Domain.BotConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEmployee.Infrastructure.Persistence;

internal sealed class BotEntityConfiguration : IEntityTypeConfiguration<Bot>
{
    public void Configure(EntityTypeBuilder<Bot> builder)
    {
        builder.ToTable("Bots");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.ExternalIntegrationId).IsRequired().HasMaxLength(512);
        builder.Property(e => e.Channel).IsRequired();
        builder.Property(e => e.IsEnabled).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.PersonaId).IsRequired();
        builder.Property(e => e.BehaviorId).IsRequired();
        builder.Property(e => e.LanguageProfileId).IsRequired();

        builder.HasOne<Persona>()
            .WithMany()
            .HasForeignKey(e => e.PersonaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Behavior>()
            .WithMany()
            .HasForeignKey(e => e.BehaviorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LanguageProfile>()
            .WithMany()
            .HasForeignKey(e => e.LanguageProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.Channel, e.ExternalIntegrationId }).IsUnique();
    }
}

internal sealed class BotIntegrationConfiguration : IEntityTypeConfiguration<BotIntegration>
{
    public void Configure(EntityTypeBuilder<BotIntegration> builder)
    {
        builder.ToTable("BotIntegrations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Channel).IsRequired().HasMaxLength(64);
        builder.Property(e => e.ExternalId).IsRequired().HasMaxLength(512);
        builder.Property(e => e.IsEnabled).IsRequired();

        builder.HasOne<Bot>()
            .WithMany()
            .HasForeignKey(e => e.BotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.Channel, e.ExternalId }).IsUnique();
    }
}

internal sealed class PersonaConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        builder.ToTable("Personas");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(256);

        builder.OwnsOne(e => e.Prompts, ps =>
        {
            ps.Property(p => p.System)
                .HasColumnName("SystemPrompt")
                .IsRequired();

            ps.Property(p => p.Judge)
                .HasColumnName("JudgePrompt")
                .IsRequired();

            ps.Property(p => p.Lead)
                .HasColumnName("LeadPrompt")
                .IsRequired();
        });

        builder.Property(e => e.ClassificationSchema)
            .HasColumnName("ClassificationSchemaJson")
            .HasConversion(
                v => ClassificationSchemaConverters.ToJson(v),
                v => ClassificationSchemaConverters.FromJson(v))
            .Metadata.SetValueComparer(ClassificationSchemaConverters.CreateComparer());
    }
}

internal sealed class BehaviorConfiguration : IEntityTypeConfiguration<Behavior>
{
    public void Configure(EntityTypeBuilder<Behavior> builder)
    {
        builder.ToTable("Behaviors");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.JudgeContextMessageCount).IsRequired();
        builder.Property(e => e.JudgePerMessageMaxChars).IsRequired();
        builder.Property(e => e.JudgeCommandPrefix).IsRequired().HasMaxLength(256);
        builder.Property(e => e.ExcludeCommandsFromJudgeContext).IsRequired();
        builder.Property(e => e.OnboardingFirstMessageOnly).IsRequired();

        builder.OwnsOne(e => e.LeadFlow, lf =>
        {
            lf.Property(l => l.FollowUpIndex).HasColumnName("LeadFollowUpIndex");
            lf.Property(l => l.CaptureIndex).HasColumnName("LeadCaptureIndex");

            lf.Property(l => l.AnswerKeys)
                .HasColumnName("AnswerKeysJson")
                .HasConversion(
                    v => AnswerKeysConverters.ToJson(v),
                    v => AnswerKeysConverters.FromJson(v))
                .Metadata.SetValueComparer(AnswerKeysConverters.CreateComparer());
        });

        builder.Property(e => e.AutomationRules)
            .HasColumnName("AutomationRulesJson")
            .HasConversion(
                v => AutomationRulesConverters.ToJson(v),
                v => AutomationRulesConverters.FromJson(v))
            .Metadata.SetValueComparer(AutomationRulesConverters.CreateComparer());
    }
}

internal sealed class LanguageProfileConfiguration : IEntityTypeConfiguration<LanguageProfile>
{
    public void Configure(EntityTypeBuilder<LanguageProfile> builder)
    {
        builder.ToTable("LanguageProfiles");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Locale).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Formality).IsRequired();

        builder.Property(e => e.OnboardingGoalQuestion).IsRequired();
        builder.Property(e => e.ExperienceFollowUpQuestion).IsRequired();
        builder.Property(e => e.LeadThanksMessage).IsRequired();
        builder.Property(e => e.JudgeNoConversationMessage).IsRequired();
        builder.Property(e => e.JudgeNotEnoughContextMessage).IsRequired();
        builder.Property(e => e.JudgeResultTemplate).IsRequired();
        builder.Property(e => e.GenericErrorMessage).IsRequired();
        builder.Property(e => e.ReactivationMessage).IsRequired();

        builder.HasIndex(e => e.Locale);
    }
}

internal sealed class PromptTemplateConfiguration : IEntityTypeConfiguration<PromptTemplate>
{
    public void Configure(EntityTypeBuilder<PromptTemplate> builder)
    {
        builder.ToTable("PromptTemplates");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Template).IsRequired();

        builder.HasIndex(e => e.Name).IsUnique();
    }
}
