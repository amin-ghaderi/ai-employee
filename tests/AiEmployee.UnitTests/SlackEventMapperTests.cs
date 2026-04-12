using AiEmployee.Application.Messaging;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Integrations.Slack;

namespace AiEmployee.UnitTests;

public sealed class SlackEventMapperTests
{
    private static BotIntegration SlackIntegration(string externalId = "https://example.com/api/slack/events/id") =>
        new(Guid.NewGuid(), Guid.NewGuid(), "slack", externalId, true);

    [Fact]
    public void MapToIncomingMessage_maps_user_text_and_integration_external_id()
    {
        var integration = SlackIntegration("https://hooks.example.com/slack");
        var request = new SlackEventRequest
        {
            Type = "event_callback",
            Event = new SlackEvent
            {
                Type = "message",
                User = "U123",
                Text = "  hello slack  ",
                Channel = "C999",
                Ts = "123.456",
            },
        };

        var msg = SlackEventMapper.MapToIncomingMessage(request, integration);

        Assert.NotNull(msg);
        Assert.Equal("slack", msg!.Channel);
        Assert.Equal("U123", msg.ExternalUserId);
        Assert.Equal("C999", msg.ExternalChatId);
        Assert.Equal("hello slack", msg.Text);
        Assert.NotNull(msg.Metadata);
        Assert.Equal("https://hooks.example.com/slack", msg.Metadata![IncomingMessageMetadataKeys.IntegrationExternalId]);
    }

    [Fact]
    public void MapToIncomingMessage_uses_thread_ts_in_external_chat_id()
    {
        var integration = SlackIntegration();
        var request = new SlackEventRequest
        {
            Type = "event_callback",
            Event = new SlackEvent
            {
                Type = "message",
                User = "U1",
                Text = "in thread",
                Channel = "C1",
                Ts = "2.2",
                ThreadTs = "1.1",
            },
        };

        var msg = SlackEventMapper.MapToIncomingMessage(request, integration);

        Assert.NotNull(msg);
        Assert.Equal("C1|1.1", msg!.ExternalChatId);
    }

    [Fact]
    public void MapToIncomingMessage_returns_null_for_bot_messages()
    {
        var integration = SlackIntegration();
        var request = new SlackEventRequest
        {
            Type = "event_callback",
            Event = new SlackEvent
            {
                Type = "message",
                User = "U1",
                Text = "x",
                Channel = "C1",
                BotId = "B123",
            },
        };

        Assert.Null(SlackEventMapper.MapToIncomingMessage(request, integration));
    }

    [Fact]
    public void MapToIncomingMessage_returns_null_for_non_message_event()
    {
        var integration = SlackIntegration();
        var request = new SlackEventRequest
        {
            Type = "event_callback",
            Event = new SlackEvent { Type = "reaction_added", User = "U1", Channel = "C1" },
        };

        Assert.Null(SlackEventMapper.MapToIncomingMessage(request, integration));
    }

    [Fact]
    public void MapToIncomingMessage_returns_null_when_event_missing()
    {
        var integration = SlackIntegration();
        var request = new SlackEventRequest { Type = "event_callback", Event = null };

        Assert.Null(SlackEventMapper.MapToIncomingMessage(request, integration));
    }

    [Fact]
    public void MapToIncomingMessage_returns_null_when_text_empty()
    {
        var integration = SlackIntegration();
        var request = new SlackEventRequest
        {
            Type = "event_callback",
            Event = new SlackEvent
            {
                Type = "message",
                User = "U1",
                Text = "   ",
                Channel = "C1",
            },
        };

        Assert.Null(SlackEventMapper.MapToIncomingMessage(request, integration));
    }

    [Fact]
    public void MapToIncomingMessage_normalizes_integration_channel_for_metadata_resolution()
    {
        var integration = new BotIntegration(Guid.NewGuid(), Guid.NewGuid(), "Slack-Events", "https://x/y", true);
        var request = new SlackEventRequest
        {
            Type = "event_callback",
            Event = new SlackEvent
            {
                Type = "message",
                User = "U1",
                Text = "hi",
                Channel = "C1",
            },
        };

        var msg = SlackEventMapper.MapToIncomingMessage(request, integration);

        Assert.NotNull(msg);
        Assert.Equal("slack-events", msg!.Channel);
    }
}
