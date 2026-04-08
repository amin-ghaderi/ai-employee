using AiEmployee.Application.Admin;
using AiEmployee.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("admin/test")]
public sealed class AdminTestController : ControllerBase
{
    private readonly IAdminTestService _adminTestService;
    private readonly IJudgeExecutionService _judgeExecutionService;
    private readonly IPersonaRepository _personaRepository;
    private readonly IBehaviorRepository _behaviorRepository;
    private readonly ILeadExecutionService _leadExecutionService;
    private readonly RealFlowTestService _realFlowTestService;

    public AdminTestController(
        IAdminTestService adminTestService,
        IJudgeExecutionService judgeExecutionService,
        IPersonaRepository personaRepository,
        IBehaviorRepository behaviorRepository,
        ILeadExecutionService leadExecutionService,
        RealFlowTestService realFlowTestService)
    {
        _adminTestService = adminTestService;
        _judgeExecutionService = judgeExecutionService;
        _personaRepository = personaRepository;
        _behaviorRepository = behaviorRepository;
        _leadExecutionService = leadExecutionService;
        _realFlowTestService = realFlowTestService;
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

    [HttpPost("judge-with-debug")]
    public async Task<ActionResult<JudgeExecutionResult>> JudgeWithDebug(
        [FromBody] TestIntegrationJudgeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _judgeExecutionService.ExecuteWithDebugAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("lead-with-debug")]
    public async Task<ActionResult<LeadExecutionResult>> RunLeadWithDebug(
        [FromBody] TestLeadWithDebugRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PersonaId == Guid.Empty || request.BehaviorId == Guid.Empty)
            return BadRequest("PersonaId and BehaviorId are required");

        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken);
        if (persona is null)
            return NotFound("Persona not found");

        var behavior = await _behaviorRepository.GetByIdAsync(request.BehaviorId, cancellationToken);
        if (behavior is null)
            return NotFound("Behavior not found");

        var answers = request.Answers ?? [];
        var answerKeys = request.AnswerKeys ?? behavior.LeadFlow.AnswerKeys.ToList();

        var result = await _leadExecutionService.ExecuteWithDebugAsync(
            persona,
            behavior,
            answers,
            answerKeys,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("real-flow")]
    public async Task<ActionResult<RealFlowTestResult>> RealFlow(
        [FromBody] RealFlowTestRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _realFlowTestService.ExecuteAsync(request, cancellationToken);
        return Ok(result);
    }
}
