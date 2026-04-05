using AiEmployee.Application.Admin;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("admin/config")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminConfigService _adminConfigService;

    public AdminController(IAdminConfigService adminConfigService)
    {
        _adminConfigService = adminConfigService;
    }

    [HttpGet("{botId:guid}")]
    public async Task<IActionResult> GetConfig(Guid botId, CancellationToken cancellationToken)
    {
        try
        {
            var config = await _adminConfigService.GetConfigAsync(botId, cancellationToken);
            return Ok(config);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{botId:guid}")]
    public async Task<IActionResult> UpdateConfig(
        Guid botId,
        [FromBody] UpdateBotConfigRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _adminConfigService.UpdateConfigAsync(botId, request, cancellationToken);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }
}
