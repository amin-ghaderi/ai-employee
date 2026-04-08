using AiEmployee.Application.Admin;

namespace AiEmployee.Application.Messaging;

public sealed class CapturingOutgoingClientDecorator : IOutgoingMessageClient
{
    private readonly IOutgoingMessageClient _inner;
    private readonly RealFlowTestContext _testContext;

    public CapturingOutgoingClientDecorator(
        IOutgoingMessageClient inner,
        RealFlowTestContext testContext)
    {
        _inner = inner;
        _testContext = testContext;
    }

    public async Task SendMessageAsync(string channel, string externalChatId, string text)
    {
        if (_testContext.IsActive)
        {
            _testContext.CapturedMessages.Add(
                new CapturedOutgoingMessage(channel, externalChatId, text, DateTimeOffset.UtcNow));
            return;
        }

        await _inner.SendMessageAsync(channel, externalChatId, text);
    }
}
