using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Rules;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rules")]
public sealed class RulesController(IRuleService ruleService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<RuleResponse>> GetAll(CancellationToken cancellationToken) =>
        ruleService.GetAllAsync(cancellationToken);

    [HttpPost]
    public Task<RuleResponse> Create([FromBody] CreateRuleRequest request, CancellationToken cancellationToken) =>
        ruleService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<RuleResponse> Update(Guid id, [FromBody] UpdateRuleRequest request, CancellationToken cancellationToken) =>
        ruleService.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await ruleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
