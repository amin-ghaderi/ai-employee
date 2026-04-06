using AiEmployee.Application.Admin;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("admin/test")]
public sealed class AdminTestController : ControllerBase
{
    private readonly IAdminTestService _adminTestService;

    public AdminTestController(IAdminTestService adminTestService)
    {
        _adminTestService = adminTestService;
    }

    [HttpPost("judge")]
    public async Task<ActionResult<TestJudgeResponse>> Judge(
        [FromBody] TestJudgeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _adminTestService.JudgeAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    [HttpPost("integration")]
    public async Task<ActionResult<TestJudgeResponse>> TestByIntegration(
        [FromBody] TestIntegrationJudgeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _adminTestService.JudgeByIntegrationAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }
}
