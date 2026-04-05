using System.Text.Json;
using AiEmployee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEmployee.Infrastructure.Persistence;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(e => e.Id);

        var tagsComparer = new ValueComparer<List<string>>(
            (a, b) => a!.SequenceEqual(b!),
            c => c.Aggregate(0, (h, x) => HashCode.Combine(h, x.GetHashCode(StringComparison.Ordinal))),
            c => c.ToList());

        builder.Property(e => e.Tags)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new())
            .Metadata.SetValueComparer(tagsComparer);
    }
}

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(e => e.Id);

        builder.HasMany(c => c.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(e => e.Id);
    }
}

internal sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("Leads");
        builder.HasKey(e => e.Id);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        var answersComparer = new ValueComparer<Dictionary<string, string>>(
            (a, b) => a!.Count == b!.Count && a.Keys.All(k =>
                b.ContainsKey(k) && string.Equals(a[k], b[k], StringComparison.Ordinal)),
            c => c.Aggregate(0, (h, kv) => HashCode.Combine(h, kv.Key.GetHashCode(StringComparison.Ordinal), kv.Value.GetHashCode(StringComparison.Ordinal))),
            c => new Dictionary<string, string>(c));

        builder.Property(e => e.Answers)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions) ?? new())
            .Metadata.SetValueComparer(answersComparer);
    }
}

internal sealed class JudgmentConfiguration : IEntityTypeConfiguration<Judgment>
{
    public void Configure(EntityTypeBuilder<Judgment> builder)
    {
        builder.ToTable("Judgments");
        builder.HasKey(e => e.Id);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(e => e.ConversationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
