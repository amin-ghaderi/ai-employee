using AiEmployee.Application.Dtos.Settings;
using AiEmployee.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("admin/settings/public-base-url")]
public sealed class AdminSettingsController : ControllerBase
{
    private readonly IAdminSettingsService _adminSettings;

    public AdminSettingsController(IAdminSettingsService adminSettings)
    {
        _adminSettings = adminSettings;
    }

    /// <summary>Returns the effective public base URL (database override or configuration).</summary>
    [HttpGet]
    public async Task<ActionResult<PublicBaseUrlDto>> Get(CancellationToken cancellationToken)
    {
        var dto = await _adminSettings.GetPublicBaseUrlAsync(cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    /// <summary>Sets or clears the database override for the public base URL.</summary>
    [HttpPut]
    public async Task<ActionResult<PublicBaseUrlDto>> Put(
        [FromBody] UpdatePublicBaseUrlRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _adminSettings
                .SetPublicBaseUrlAsync(request?.PublicBaseUrl, cancellationToken)
                .ConfigureAwait(false);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Removes the database override so configuration is used.</summary>
    [HttpDelete]
    public async Task<ActionResult<PublicBaseUrlDto>> Delete(CancellationToken cancellationToken)
    {
        await _adminSettings.ClearPublicBaseUrlAsync(cancellationToken).ConfigureAwait(false);
        var dto = await _adminSettings.GetPublicBaseUrlAsync(cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }
}
