using AiEmployee.Application.Bots;
using AiEmployee.Application.Dtos.Bots;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("admin/bots")]
public sealed class AdminBotController : ControllerBase
{
    private readonly IBotAdminService _botAdminService;

    public AdminBotController(IBotAdminService botAdminService)
    {
        _botAdminService = botAdminService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBotRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _botAdminService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (BotValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BotDto>>> List(CancellationToken cancellationToken)
    {
        var items = await _botAdminService.ListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BotDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _botAdminService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (dto is null)
            return NotFound();
        return Ok(dto);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBotRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _botAdminService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return Ok(dto);
        }
        catch (BotValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id:guid}/assignments")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] BotAssignmentsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _botAdminService.AssignAsync(id, request, cancellationToken).ConfigureAwait(false);
            return Ok(dto);
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
            var dto = await _botAdminService.EnableAsync(id, cancellationToken).ConfigureAwait(false);
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
            var dto = await _botAdminService.DisableAsync(id, cancellationToken).ConfigureAwait(false);
            return Ok(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
