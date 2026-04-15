using AiEmployee.Application.Messaging;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.UnitTests;

public sealed class ConversationIdentityTests
{
    [Fact]
    public void Non_telegram_uses_external_chat_id_only()
    {
        var msg = new IncomingMessage("slack", "U1", "C9", "hi", null);
        Assert.Equal("C9", ConversationIdentity.ResolveConversationId(msg));
    }

    [Fact]
    public void Telegram_without_scope_falls_back_to_chat_id_only()
    {
        var msg = new IncomingMessage(BotIntegrationChannelNames.Telegram, "1", "999", "hi", null);
        Assert.Equal("999", ConversationIdentity.ResolveConversationId(msg));
    }

    [Fact]
    public void Telegram_same_chat_different_bot_scopes_produce_distinct_conversation_ids()
    {
        var chat = "424242";
        var a = new IncomingMessage(
            BotIntegrationChannelNames.Telegram,
            chat,
            chat,
            "x",
            new Dictionary<string, string>
            {
                [IncomingMessageMetadataKeys.TelegramBotScopeKey] = "i:11111111111111111111111111111111",
            });
        var b = new IncomingMessage(
            BotIntegrationChannelNames.Telegram,
            chat,
            chat,
            "x",
            new Dictionary<string, string>
            {
                [IncomingMessageMetadataKeys.TelegramBotScopeKey] = "i:22222222222222222222222222222222",
            });

        var idA = ConversationIdentity.ResolveConversationId(a);
        var idB = ConversationIdentity.ResolveConversationId(b);

        Assert.NotEqual(idA, idB);
        Assert.Contains(chat, idA, StringComparison.Ordinal);
        Assert.Contains(chat, idB, StringComparison.Ordinal);
        Assert.StartsWith("telegram|", idA, StringComparison.Ordinal);
        Assert.StartsWith("telegram|", idB, StringComparison.Ordinal);
    }

    /// <summary>Matches the documented isolation scenario: same ExternalChatId, different <c>TelegramBotScopeKey</c>.</summary>
    [Fact]
    public void Telegram_isolation_scenario_two_bots_same_chat_produce_expected_composite_ids()
    {
        var botA = new IncomingMessage(
            BotIntegrationChannelNames.Telegram,
            "user_1",
            "123456789",
            "Hello from Bot A",
            new Dictionary<string, string>
            {
                [IncomingMessageMetadataKeys.IntegrationExternalId] = "botA",
                [IncomingMessageMetadataKeys.TelegramBotScopeKey] = "i:botA",
            });

        var botB = new IncomingMessage(
            BotIntegrationChannelNames.Telegram,
            "user_1",
            "123456789",
            "Hello from Bot B",
            new Dictionary<string, string>
            {
                [IncomingMessageMetadataKeys.IntegrationExternalId] = "botB",
                [IncomingMessageMetadataKeys.TelegramBotScopeKey] = "i:botB",
            });

        Assert.Equal("telegram|i:botA|123456789", ConversationIdentity.ResolveConversationId(botA));
        Assert.Equal("telegram|i:botB|123456789", ConversationIdentity.ResolveConversationId(botB));
    }
}
