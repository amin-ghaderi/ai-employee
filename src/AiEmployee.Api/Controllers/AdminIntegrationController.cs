using AiEmployee.Application.Dtos.Integrations;
using AiEmployee.Application.Integrations;
using AiEmployee.Application.Options;
using AiEmployee.Domain.BotConfiguration;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("admin/integrations")]
public sealed class AdminIntegrationController : ControllerBase
{
    private readonly IBotIntegrationAdminService _integrationAdminService;
    private readonly IIntegrationWebhookApplicationService _integrationWebhook;
    private readonly IPublicBaseUrlProvider _publicBaseUrl;

    public AdminIntegrationController(
        IBotIntegrationAdminService integrationAdminService,
        IIntegrationWebhookApplicationService integrationWebhook,
        IPublicBaseUrlProvider publicBaseUrl)
    {
        _integrationAdminService = integrationAdminService;
        _integrationWebhook = integrationWebhook;
        _publicBaseUrl = publicBaseUrl;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBotIntegrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _integrationAdminService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (BotIntegrationValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BotIntegrationDto>>> List(
        [FromQuery] Guid? botId,
        CancellationToken cancellationToken)
    {
        var items = await _integrationAdminService.ListAsync(botId, cancellationToken).ConfigureAwait(false);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BotIntegrationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _integrationAdminService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (dto is null)
            return NotFound();
        return Ok(dto);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBotIntegrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _integrationAdminService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return Ok(dto);
        }
        catch (BotIntegrationValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _integrationAdminService.EnableAsync(id, cancellationToken).ConfigureAwait(false);
            return Ok(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _integrationAdminService.DisableAsync(id, cancellationToken).ConfigureAwait(false);
            return Ok(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Registers outbound webhook for this integration when the provider supports it (Telegram in Phase 1).</summary>
    [HttpPost("{id:guid}/sync-webhook")]
    [ProducesResponseType(typeof(AdminTelegramWebhookSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminTelegramWebhookSummaryDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AdminTelegramWebhookSummaryDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(AdminTelegramWebhookSummaryDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SyncWebhook(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _integrationWebhook.SyncWebhookAsync(id, cancellationToken).ConfigureAwait(false);
        if (result.Success)
        {
            var syncedAt = DateTime.UtcNow;
            return Ok(ToSummaryFromSync(result, syncedAt));
        }

        if (result.FailureCategory == IntegrationWebhookFailureCategory.IntegrationNotFound)
        {
            return NotFound(new AdminTelegramWebhookSummaryDto(
                null,
                "not_found",
                result.Message ?? "Integration not found.",
                null));
        }

        var dto = ToSummaryFromSync(result, lastSyncedAt: null);
        return MapSyncFailureToActionResult(result, dto);
    }

    /// <summary>Reads webhook registration status from the integration's provider.</summary>
    [HttpGet("{id:guid}/webhook-status")]
    [ProducesResponseType(typeof(AdminTelegramWebhookSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminTelegramWebhookSummaryDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AdminTelegramWebhookSummaryDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(AdminTelegramWebhookSummaryDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetWebhookStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var expected = await TryGetExpectedWebhookUrlAsync(id, cancellationToken).ConfigureAwait(false);
        var result = await _integrationWebhook.GetWebhookStatusAsync(id, cancellationToken).ConfigureAwait(false);
        if (result.Success)
            return Ok(ToSummaryFromInfoSuccess(result, expected));

        if (result.FailureCategory == IntegrationWebhookFailureCategory.IntegrationNotFound)
        {
            return NotFound(new AdminTelegramWebhookSummaryDto(
                null,
                "not_found",
                result.Message ?? "Integration not found.",
                null));
        }

        var dto = ToSummaryFromInfoFailure(result, expected);
        return MapInfoFailureToActionResult(result, dto);
    }

    /// <summary>Removes webhook registration for this integration's provider.</summary>
    [HttpDelete("{id:guid}/webhook")]
    [ProducesResponseType(typeof(AdminTelegramWebhookSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminTelegramWebhookSummaryDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AdminTelegramWebhookSummaryDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(AdminTelegramWebhookSummaryDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> DeleteWebhook(
        Guid id,
        [FromQuery] bool dropPendingUpdates = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _integrationWebhook.DeleteWebhookAsync(id, dropPendingUpdates, cancellationToken)
            .ConfigureAwait(false);
        if (result.Success)
            return Ok(new AdminTelegramWebhookSummaryDto(null, "deleted", null, null));

        if (result.FailureCategory == IntegrationWebhookFailureCategory.IntegrationNotFound)
        {
            return NotFound(new AdminTelegramWebhookSummaryDto(
                null,
                "not_found",
                result.Message ?? "Integration not found.",
                null));
        }

        var dto = new AdminTelegramWebhookSummaryDto(
            null,
            "error",
            result.Message ?? result.ProviderDescription,
            null);
        return MapDeleteFailureToActionResult(result, dto);
    }

    /// <summary>Telegram: public base + integration route. Generic webhook: configured URL in <c>ExternalId</c>.</summary>
    private async Task<string?> TryGetExpectedWebhookUrlAsync(Guid integrationId, CancellationToken cancellationToken)
    {
        var dto = await _integrationAdminService.GetByIdAsync(integrationId, cancellationToken).ConfigureAwait(false);
        if (dto is null)
            return null;

        if (string.Equals(dto.Provider, IntegrationProviders.Slack, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var b = _publicBaseUrl.GetPublicBaseUrl();
                return string.IsNullOrEmpty(b)
                    ? null
                    : $"{b.TrimEnd('/')}/api/slack/events/{integrationId}";
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        if (string.Equals(dto.Provider, IntegrationProviders.GenericWebhook, StringComparison.OrdinalIgnoreCase)
            || string.Equals(dto.Provider, IntegrationProviders.WhatsApp, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalId))
                return null;
            var ext = dto.ExternalId.Trim();
            return Uri.TryCreate(ext, UriKind.Absolute, out _) ? ext : null;
        }

        try
        {
            var b = _publicBaseUrl.GetPublicBaseUrl();
            return string.IsNullOrEmpty(b) ? null : $"{b}/api/telegram/webhook/{integrationId}";
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private IActionResult MapSyncFailureToActionResult(
        IntegrationWebhookSyncResult result,
        AdminTelegramWebhookSummaryDto dto) =>
        result.FailureCategory switch
        {
            IntegrationWebhookFailureCategory.BadRequestGuard => BadRequest(dto),
            IntegrationWebhookFailureCategory.UnsupportedProvider => BadRequest(dto),
            IntegrationWebhookFailureCategory.ClientConfiguration => StatusCode(
                StatusCodes.Status400BadRequest,
                dto),
            IntegrationWebhookFailureCategory.InternalError => StatusCode(
                StatusCodes.Status500InternalServerError,
                dto),
            _ => StatusCode(StatusCodeForUpstreamSyncFailure(result.Message), dto),
        };

    private IActionResult MapInfoFailureToActionResult(
        IntegrationWebhookInfoResult result,
        AdminTelegramWebhookSummaryDto dto) =>
        result.FailureCategory switch
        {
            IntegrationWebhookFailureCategory.BadRequestGuard => BadRequest(dto),
            IntegrationWebhookFailureCategory.UnsupportedProvider => BadRequest(dto),
            IntegrationWebhookFailureCategory.ClientConfiguration => StatusCode(
                StatusCodes.Status400BadRequest,
                dto),
            IntegrationWebhookFailureCategory.InternalError => StatusCode(
                StatusCodes.Status500InternalServerError,
                dto),
            _ => StatusCode(StatusCodeForUpstreamInfoFailure(result.Message), dto),
        };

    private IActionResult MapDeleteFailureToActionResult(
        IntegrationWebhookDeleteResult result,
        AdminTelegramWebhookSummaryDto dto) =>
        result.FailureCategory switch
        {
            IntegrationWebhookFailureCategory.BadRequestGuard => BadRequest(dto),
            IntegrationWebhookFailureCategory.UnsupportedProvider => BadRequest(dto),
            IntegrationWebhookFailureCategory.ClientConfiguration => StatusCode(
                StatusCodes.Status400BadRequest,
                dto),
            IntegrationWebhookFailureCategory.InternalError => StatusCode(
                StatusCodes.Status500InternalServerError,
                dto),
            _ => StatusCode(StatusCodeForUpstreamInfoFailure(result.Message), dto),
        };

    private static AdminTelegramWebhookSummaryDto ToSummaryFromSync(IntegrationWebhookSyncResult r, DateTime? lastSyncedAt)
    {
        if (r.Success)
            return new AdminTelegramWebhookSummaryDto(r.ConfiguredWebhookUrl, "synced", null, lastSyncedAt);

        return new AdminTelegramWebhookSummaryDto(
            r.ConfiguredWebhookUrl,
            "error",
            r.Message ?? r.ProviderDescription,
            null);
    }

    private static AdminTelegramWebhookSummaryDto ToSummaryFromInfoFailure(
        IntegrationWebhookInfoResult r,
        string? expectedWebhookUrl)
    {
        return new AdminTelegramWebhookSummaryDto(
            expectedWebhookUrl,
            "error",
            r.Message ?? r.ProviderDescription,
            null);
    }

    private static AdminTelegramWebhookSummaryDto ToSummaryFromInfoSuccess(
        IntegrationWebhookInfoResult r,
        string? expectedWebhookUrl)
    {
        var url = r.Info?.Url;
        if (string.IsNullOrWhiteSpace(url))
        {
            return new AdminTelegramWebhookSummaryDto(
                expectedWebhookUrl,
                "not_registered",
                r.Info?.LastErrorMessage,
                null);
        }

        var lastErr = r.Info?.LastErrorMessage;
        string status;
        if (expectedWebhookUrl is null)
        {
            status = "active";
        }
        else if (string.Equals(url, expectedWebhookUrl, StringComparison.Ordinal))
        {
            status = "active";
        }
        else
        {
            status = "mismatch";
        }

        return new AdminTelegramWebhookSummaryDto(url, status, lastErr, null);
    }

    private static bool IsClientConfigurationError(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return false;

        return message.Contains("PublicBaseUrl", StringComparison.OrdinalIgnoreCase)
            || message.Contains("must use HTTPS in Production", StringComparison.OrdinalIgnoreCase)
            || message.Contains("absolute URI", StringComparison.OrdinalIgnoreCase);
    }

    private static int StatusCodeForUpstreamSyncFailure(string? message) =>
        IsClientConfigurationError(message)
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status502BadGateway;

    private static int StatusCodeForUpstreamInfoFailure(string? message) =>
        IsClientConfigurationError(message)
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status502BadGateway;
}
