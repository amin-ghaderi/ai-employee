using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Integrations.Slack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AiEmployee.Api.Controllers;

/// <summary>Slack Events API: URL verification, signature validation, and dispatch to <see cref="IIncomingMessageHandler"/>.</summary>
[ApiController]
[Route("api/slack/events")]
public sealed class SlackWebhookController : ControllerBase
{
    private const int MaxRequestAgeSeconds = 300;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly SlackSettings _settings;
    private readonly IIncomingMessageHandler _incomingMessageHandler;
    private readonly IBotIntegrationRepository _integrations;

    public SlackWebhookController(
        IOptions<SlackSettings> options,
        IIncomingMessageHandler incomingMessageHandler,
        IBotIntegrationRepository integrations)
    {
        _settings = options.Value;
        _incomingMessageHandler = incomingMessageHandler;
        _integrations = integrations;
    }

    [HttpPost]
    public Task<IActionResult> Receive(CancellationToken cancellationToken) =>
        ReceiveCore(integrationId: null, cancellationToken);

    [HttpPost("{integrationId:guid}")]
    public Task<IActionResult> ReceiveForIntegration(Guid integrationId, CancellationToken cancellationToken) =>
        ReceiveCore(integrationId, cancellationToken);

    private async Task<IActionResult> ReceiveCore(Guid? integrationId, CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        Request.Body.Position = 0;
        string body;
        using (var reader = new StreamReader(
                   Request.Body,
                   Encoding.UTF8,
                   detectEncodingFromByteOrderMarks: false,
                   bufferSize: 1024,
                   leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        Request.Body.Position = 0;

        if (!IsValidSlackRequest(Request.Headers, body))
            return Unauthorized();

        SlackEventRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<SlackEventRequest>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        if (request is null || string.IsNullOrEmpty(request.Type))
            return BadRequest();

        if (string.Equals(request.Type, "url_verification", StringComparison.Ordinal))
        {
            if (string.IsNullOrEmpty(request.Challenge))
                return BadRequest();
            return Ok(new { challenge = request.Challenge });
        }

        if (!string.Equals(request.Type, "event_callback", StringComparison.Ordinal))
            return Ok();

        var resolvedId = await ResolveIntegrationIdAsync(integrationId, cancellationToken).ConfigureAwait(false);
        if (resolvedId is null)
            return BadRequest();

        var integration = await _integrations.GetByIdAsync(resolvedId.Value, cancellationToken).ConfigureAwait(false);
        if (integration is null || !integration.IsEnabled)
            return NotFound();

        if (!string.Equals(
                IntegrationProviders.TryResolveFromChannel(integration.Channel),
                IntegrationProviders.Slack,
                StringComparison.Ordinal))
            return BadRequest();

        var incoming = SlackEventMapper.MapToIncomingMessage(request, integration);
        if (incoming is not null)
            await _incomingMessageHandler.HandleAsync(incoming).ConfigureAwait(false);

        return Ok();
    }

    private async Task<Guid?> ResolveIntegrationIdAsync(Guid? routeIntegrationId, CancellationToken cancellationToken)
    {
        if (routeIntegrationId is { } rid && rid != Guid.Empty)
            return rid;

        if (Request.Headers.TryGetValue("X-Integration-Id", out var headerValues))
        {
            var raw = headerValues.FirstOrDefault();
            if (Guid.TryParse(raw, out var fromHeader) && fromHeader != Guid.Empty)
                return fromHeader;
        }

        var all = await _integrations.ListAsync(botId: null, cancellationToken).ConfigureAwait(false);
        var slackRows = all
            .Where(i =>
                i.IsEnabled
                && !string.IsNullOrWhiteSpace(i.ExternalId)
                && string.Equals(
                    IntegrationProviders.TryResolveFromChannel(i.Channel),
                    IntegrationProviders.Slack,
                    StringComparison.Ordinal))
            .ToList();

        if (slackRows.Count == 1)
            return slackRows[0].Id;

        return null;
    }

    private bool IsValidSlackRequest(IHeaderDictionary headers, string body)
    {
        if (string.IsNullOrEmpty(_settings.SigningSecret))
            return false;

        var timestamp = headers["X-Slack-Request-Timestamp"].ToString();
        var signature = headers["X-Slack-Signature"].ToString();

        if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signature))
            return false;

        if (!long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tsUnix))
            return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - tsUnix) > MaxRequestAgeSeconds)
            return false;

        var baseString = $"v0:{timestamp}:{body}";
        var secret = Encoding.UTF8.GetBytes(_settings.SigningSecret);

        using var hmac = new HMACSHA256(secret);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
        var computedSignature = "v0=" + Convert.ToHexString(hash).ToLowerInvariant();

        var sigBytes = Encoding.UTF8.GetBytes(computedSignature);
        var headerBytes = Encoding.UTF8.GetBytes(signature);
        if (sigBytes.Length != headerBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(sigBytes, headerBytes);
    }
}
