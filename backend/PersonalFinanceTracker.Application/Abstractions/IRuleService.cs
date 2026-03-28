using PersonalFinanceTracker.Application.DTOs.Rules;
using PersonalFinanceTracker.Domain.Entities;

namespace PersonalFinanceTracker.Application.Abstractions;

public interface IRuleService
{
    Task<IReadOnlyCollection<RuleResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<RuleResponse> CreateAsync(CreateRuleRequest request, CancellationToken cancellationToken);
    Task<RuleResponse> UpdateAsync(Guid id, UpdateRuleRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task ApplyRulesAsync(Transaction transaction, CancellationToken cancellationToken);
}
