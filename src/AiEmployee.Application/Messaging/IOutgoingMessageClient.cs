namespace AiEmployee.Application.Messaging;

public interface IOutgoingMessageClient
{
    Task SendMessageAsync(string channel, string externalChatId, string text);
}
