using AiEmployee.Application.Dtos.Integrations;
using AiEmployee.Application.Integrations;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("admin/integrations")]
public sealed class AdminIntegrationController : ControllerBase
{
    private readonly IBotIntegrationAdminService _integrationAdminService;

    public AdminIntegrationController(IBotIntegrationAdminService integrationAdminService)
    {
        _integrationAdminService = integrationAdminService;
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
}
