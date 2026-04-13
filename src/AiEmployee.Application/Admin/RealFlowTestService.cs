using System.Diagnostics;
using System.Globalization;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Messaging;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Domain.Entities;
using AiEmployee.Application.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Application.Admin;

public sealed class RealFlowTestService
{
    private readonly RealFlowTestContext _ctx;
    private readonly IFlowTracker _flowTracker;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RealFlowTestService> _logger;

    public RealFlowTestService(
        RealFlowTestContext ctx,
        IFlowTracker flowTracker,
        IServiceScopeFactory scopeFactory,
        ILogger<RealFlowTestService> logger)
    {
        _ctx = ctx;
        _flowTracker = flowTracker;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<RealFlowTestResult> ExecuteAsync(
        RealFlowTestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _ctx.CapturedMessages.Clear();
        _ctx.FlowExecuted = null;
        _ctx.PipelineError = null;
        _ctx.DisableAutomation = request.DisableAutomation;

        var inMemoryRepo = new InMemoryConversationRepository();

        if (request.ResetConversation)
        {
            await inMemoryRepo.ReplaceMessagesAsync(
                request.ExternalChatId,
                Array.Empty<Message>(),
                cancellationToken);
        }

        var lines = request.Text
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var sw = Stopwatch.StartNew();
        Exception? error = null;
        _ctx.IsActive = true;
        try
        {
            for (var i = 0; i < lines.Count; i++)
            {
                _logger.LogInformation(
                    "RealFlowTest [{Index}/{Total}]: {Line}",
                    i + 1, lines.Count, lines[i]);

                var metadata = BuildTestMetadata(request, lines[i], i);

                var message = new IncomingMessage(
                    request.Channel,
                    request.ExternalUserId,
                    request.ExternalChatId,
                    lines[i],
                    metadata);

                using var scope = _scopeFactory.CreateScope();
                var childCtx = scope.ServiceProvider.GetRequiredService<RealFlowTestContext>();
                childCtx.IsActive = true;
                childCtx.DisableAutomation = _ctx.DisableAutomation;
                childCtx.ConversationOverride = inMemoryRepo;

                var handler = scope.ServiceProvider.GetRequiredService<IIncomingMessageHandler>();
                await handler.HandleAsync(message);

                _ctx.CapturedMessages.AddRange(childCtx.CapturedMessages);
                if (childCtx.FlowExecuted is not null)
                    _ctx.FlowExecuted = childCtx.FlowExecuted;
                if (childCtx.PipelineError is not null)
                    _ctx.PipelineError = childCtx.PipelineError;
            }
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _ctx.IsActive = false;
            sw.Stop();
        }

        return new RealFlowTestResult
        {
            FlowExecuted = _flowTracker.Get() ?? "unknown",
            Messages = _ctx.CapturedMessages.ToList(),
            LatencyMs = sw.ElapsedMilliseconds,
            Error = error?.Message ?? _ctx.PipelineError,
        };
    }

    private static Dictionary<string, string> BuildTestMetadata(
        RealFlowTestRequest request,
        string line,
        int lineOrdinal)
    {
        var colonIdx = line.IndexOf(':');
        var firstName = colonIdx > 0 ? line[..colonIdx].Trim() : "TestUser";
        if (firstName.Length == 0 || firstName.Length > 50 || firstName.StartsWith('/'))
            firstName = "TestUser";

        var meta = new Dictionary<string, string>
        {
            [IncomingMessageMetadataKeys.FirstName] = firstName,
            [IncomingMessageMetadataKeys.Username] = request.ExternalUserId,
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
        };

        if (!string.IsNullOrWhiteSpace(request.IntegrationExternalId))
            meta[IncomingMessageMetadataKeys.IntegrationExternalId] = request.IntegrationExternalId;

        if (BotIntegrationChannelNames.IsTelegramChannel(request.Channel))
        {
            meta[IncomingMessageMetadataKeys.TelegramBotScopeKey] =
                !string.IsNullOrWhiteSpace(request.IntegrationExternalId)
                    ? $"realflow:{request.IntegrationExternalId.Trim().GetHashCode(StringComparison.Ordinal):X8}"
                    : $"realflow:{request.ExternalChatId}";
            meta[IncomingMessageMetadataKeys.TelegramUpdateId] =
                (10_000_000L + lineOrdinal).ToString(CultureInfo.InvariantCulture);
        }

        return meta;
    }
}
