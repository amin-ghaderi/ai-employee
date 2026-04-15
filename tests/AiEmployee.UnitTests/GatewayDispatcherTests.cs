using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Messaging;
using AiEmployee.Application.Options;
using AiEmployee.Domain.BotConfiguration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AiEmployee.UnitTests;

public sealed class GatewayDispatcherTests
{
    private static GatewayDispatcher CreateSut(
        out Mock<IBotIntegrationRepository> integrations,
        out Mock<IOutgoingMessageClient> outgoing,
        out Mock<IGatewayTelemetry> telemetry,
        GatewayOptions? options = null)
    {
        integrations = new Mock<IBotIntegrationRepository>(MockBehavior.Strict);
        outgoing = new Mock<IOutgoingMessageClient>(MockBehavior.Strict);
        telemetry = new Mock<IGatewayTelemetry>(MockBehavior.Loose);
        return new GatewayDispatcher(
            integrations.Object,
            outgoing.Object,
            telemetry.Object,
            Options.Create(options ?? new GatewayOptions()),
            NullLogger<GatewayDispatcher>.Instance);
    }

    [Fact]
    public async Task DispatchAsync_records_not_found_when_integration_missing()
    {
        var sut = CreateSut(out var integrations, out var outgoing, out var telemetry);
        var botId = Guid.NewGuid();
        integrations
            .Setup(i => i.GetByChannelAndExternalIdAsync("telegram", "ext", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BotIntegration?)null);

        await sut.DispatchAsync(botId, "telegram", "ext", "hello", CancellationToken.None);

        outgoing.Verify(
            o => o.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        telemetry.Verify(t => t.RecordDispatchAttempt(), Times.Once);
        telemetry.Verify(t => t.RecordDispatchOutcome("integration_not_found"), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_sends_when_configured_and_records_success()
    {
        var sut = CreateSut(out var integrations, out var outgoing, out var telemetry);
        var botId = Guid.NewGuid();
        var integrationId = Guid.NewGuid();
        var integration = new BotIntegration(
            integrationId,
            botId,
            "telegram",
            "tok",
            isEnabled: true,
            gatewayChannel: "slack",
            gatewayExternalId: "C123");

        integrations
            .Setup(i => i.GetByChannelAndExternalIdAsync("telegram", "tok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(integration);

        outgoing
            .Setup(o => o.SendMessageAsync("slack", "C123", "hello"))
            .Returns(Task.CompletedTask);

        await sut.DispatchAsync(botId, "telegram", "tok", "hello", CancellationToken.None);

        outgoing.Verify(
            o => o.SendMessageAsync("slack", "C123", "hello"),
            Times.Once);
        telemetry.Verify(t => t.RecordDispatchAttempt(), Times.Once);
        telemetry.Verify(t => t.RecordDispatchOutcome("success"), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_truncates_long_message_per_options()
    {
        // Dispatcher uses max(256, MaxForwardedMessageChars); 200 -> effective cap 256.
        var sut = CreateSut(
            out var integrations,
            out var outgoing,
            out var telemetry,
            new GatewayOptions { MaxForwardedMessageChars = 200 });
        var botId = Guid.NewGuid();
        var integration = new BotIntegration(
            Guid.NewGuid(),
            botId,
            "telegram",
            "tok",
            isEnabled: true,
            gatewayChannel: "slack",
            gatewayExternalId: "C123");

        integrations
            .Setup(i => i.GetByChannelAndExternalIdAsync("telegram", "tok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(integration);

        var longText = new string('x', 300);
        outgoing
            .Setup(o => o.SendMessageAsync("slack", "C123", new string('x', 256)))
            .Returns(Task.CompletedTask);

        await sut.DispatchAsync(botId, "telegram", "tok", longText, CancellationToken.None);

        telemetry.Verify(t => t.RecordDispatchOutcome("success_truncated"), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_skips_when_bot_id_mismatch()
    {
        var sut = CreateSut(out var integrations, out var outgoing, out var telemetry);
        var integration = new BotIntegration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "telegram",
            "tok",
            isEnabled: true,
            gatewayChannel: "slack",
            gatewayExternalId: "C123");

        integrations
            .Setup(i => i.GetByChannelAndExternalIdAsync("telegram", "tok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(integration);

        await sut.DispatchAsync(Guid.NewGuid(), "telegram", "tok", "hi", CancellationToken.None);

        outgoing.Verify(
            o => o.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        telemetry.Verify(t => t.RecordDispatchOutcome("bot_mismatch"), Times.Once);
    }
}
