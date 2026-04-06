using AiEmployee.Application.Behaviors;
using AiEmployee.Application.Dtos.Behaviors;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("admin/behaviors")]
public sealed class AdminBehaviorController : ControllerBase
{
    private readonly IBehaviorAdminService _behaviorAdminService;

    public AdminBehaviorController(IBehaviorAdminService behaviorAdminService)
    {
        _behaviorAdminService = behaviorAdminService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBehaviorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _behaviorAdminService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (BehaviorValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BehaviorDto>>> List(CancellationToken cancellationToken)
    {
        var items = await _behaviorAdminService.ListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BehaviorDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _behaviorAdminService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (dto is null)
            return NotFound();
        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBehaviorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _behaviorAdminService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return Ok(dto);
        }
        catch (BehaviorValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
