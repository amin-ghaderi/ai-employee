using System.ComponentModel.DataAnnotations.Schema;
using AiEmployee.Domain.Entities;
using Pgvector;

namespace AiEmployee.Infrastructure.Persistence.Entities;

/// <summary>
/// PostgreSQL-only row for message embeddings (pgvector). Not used with SQLite.
/// </summary>
public sealed class MessageEmbeddingEntity
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    [Column(TypeName = "vector(1536)")]
    public Vector? Embedding { get; set; }

    public DateTime CreatedAt { get; set; }

    public Message? Message { get; set; }
}
