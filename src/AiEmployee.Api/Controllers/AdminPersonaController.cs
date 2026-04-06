using AiEmployee.Application.Dtos.Personas;
using AiEmployee.Application.Personas;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("admin/personas")]
public sealed class AdminPersonaController : ControllerBase
{
    private readonly IPersonaAdminService _personaAdminService;

    public AdminPersonaController(IPersonaAdminService personaAdminService)
    {
        _personaAdminService = personaAdminService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePersonaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _personaAdminService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (PersonaValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PersonaDto>>> List(CancellationToken cancellationToken)
    {
        var items = await _personaAdminService.ListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PersonaDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _personaAdminService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (dto is null)
            return NotFound();
        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePersonaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _personaAdminService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return Ok(dto);
        }
        catch (PersonaValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
