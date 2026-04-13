using AiEmployee.Application.Admin;
using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Messaging;

public sealed class TestScopedConversationRepository : IConversationRepository
{
    private readonly IConversationRepository _inner;
    private readonly RealFlowTestContext _ctx;

    public TestScopedConversationRepository(
        IConversationRepository inner,
        RealFlowTestContext ctx)
    {
        _inner = inner;
        _ctx = ctx;
    }

    private IConversationRepository Active =>
        _ctx is { IsActive: true, ConversationOverride: not null }
            ? _ctx.ConversationOverride
            : _inner;

    public Task<Conversation?> GetByIdAsync(string id) =>
        Active.GetByIdAsync(id);

    public Task SaveAsync(Conversation conversation) =>
        Active.SaveAsync(conversation);

    public Task AppendUserMessageAsync(
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default) =>
        Active.AppendUserMessageAsync(conversationId, message, cancellationToken);

    public Task ReplaceMessagesAsync(
        string conversationId,
        IReadOnlyList<Message> messages,
        CancellationToken cancellationToken = default) =>
        Active.ReplaceMessagesAsync(conversationId, messages, cancellationToken);
}
