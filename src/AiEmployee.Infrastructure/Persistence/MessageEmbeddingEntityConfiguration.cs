using AiEmployee.Domain.Entities;
using AiEmployee.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEmployee.Infrastructure.Persistence;

internal sealed class MessageEmbeddingEntityConfiguration : IEntityTypeConfiguration<MessageEmbeddingEntity>
{
    public void Configure(EntityTypeBuilder<MessageEmbeddingEntity> builder)
    {
        builder.ToTable("MessageEmbeddings");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MessageId).IsRequired();
        builder.Property(e => e.ConversationId).IsRequired().HasMaxLength(2048);
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.Property(e => e.Embedding)
            .HasColumnType("vector(1536)");

        builder.HasOne(e => e.Message)
            .WithMany()
            .HasForeignKey(e => e.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.MessageId).IsUnique();
        builder.HasIndex(e => e.ConversationId);
    }
}
