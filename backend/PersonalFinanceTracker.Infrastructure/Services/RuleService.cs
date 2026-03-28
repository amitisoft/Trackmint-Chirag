using Microsoft.EntityFrameworkCore;
using System.Globalization;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Rules;
using PersonalFinanceTracker.Application.Exceptions;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Services;

public sealed class RuleService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IAuditService auditService) : IRuleService
{
    public async Task<IReadOnlyCollection<RuleResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var items = await dbContext.Rules
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToArrayAsync(cancellationToken);

        return items.Select(ToResponse).ToArray();
    }

    public async Task<RuleResponse> CreateAsync(CreateRuleRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        ValidationGuard.AgainstBlank(request.Name, "Rule name");
        ValidationGuard.AgainstBlank(request.ConditionValue, "Condition value");

        var item = new Rule
        {
            UserId = userId,
            Name = request.Name.Trim(),
            ConditionField = request.ConditionField,
            ConditionOperator = request.ConditionOperator,
            ConditionValue = request.ConditionValue.Trim(),
            ActionType = request.ActionType,
            ActionValue = request.ActionValue.Trim(),
            Priority = Math.Clamp(request.Priority, 1, 1000),
            IsActive = request.IsActive
        };

        await dbContext.Rules.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(userId, "rule_created", nameof(Rule), item.Id, new { item.Name, item.Priority }, cancellationToken);

        return ToResponse(item);
    }

    public async Task<RuleResponse> UpdateAsync(Guid id, UpdateRuleRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var item = await dbContext.Rules.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Rule not found.");

        ValidationGuard.AgainstBlank(request.Name, "Rule name");
        ValidationGuard.AgainstBlank(request.ConditionValue, "Condition value");

        item.Name = request.Name.Trim();
        item.ConditionField = request.ConditionField;
        item.ConditionOperator = request.ConditionOperator;
        item.ConditionValue = request.ConditionValue.Trim();
        item.ActionType = request.ActionType;
        item.ActionValue = request.ActionValue.Trim();
        item.Priority = Math.Clamp(request.Priority, 1, 1000);
        item.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(userId, "rule_updated", nameof(Rule), item.Id, new { item.Name, item.Priority }, cancellationToken);

        return ToResponse(item);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var item = await dbContext.Rules.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Rule not found.");

        dbContext.Rules.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(userId, "rule_deleted", nameof(Rule), item.Id, new { item.Name }, cancellationToken);
    }

    public async Task ApplyRulesAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        var rules = await dbContext.Rules
            .Where(x => x.UserId == transaction.UserId && x.IsActive)
            .OrderBy(x => x.Priority)
            .ToArrayAsync(cancellationToken);

        if (rules.Length == 0)
        {
            return;
        }

        string? categoryName = null;
        if (transaction.CategoryId.HasValue)
        {
            categoryName = await dbContext.Categories
                .Where(x => x.Id == transaction.CategoryId.Value)
                .Select(x => x.Name)
                .SingleOrDefaultAsync(cancellationToken);
        }

        foreach (var rule in rules)
        {
            if (!ConditionMatches(rule, transaction, categoryName))
            {
                continue;
            }

            await ApplyActionAsync(rule, transaction, cancellationToken);
        }
    }

    private async Task ApplyActionAsync(Rule rule, Transaction transaction, CancellationToken cancellationToken)
    {
        switch (rule.ActionType)
        {
            case RuleActionType.SetCategory:
            {
                var normalized = rule.ActionValue.Trim();
                Guid? categoryId = null;
                if (Guid.TryParse(normalized, out var parsedCategoryId))
                {
                    categoryId = await dbContext.Categories
                        .Where(x => x.Id == parsedCategoryId && x.UserId == transaction.UserId)
                        .Select(x => (Guid?)x.Id)
                        .SingleOrDefaultAsync(cancellationToken);
                }
                else
                {
                    categoryId = await dbContext.Categories
                        .Where(x => x.UserId == transaction.UserId && x.Name.ToLower() == normalized.ToLower())
                        .Select(x => (Guid?)x.Id)
                        .SingleOrDefaultAsync(cancellationToken);
                }

                if (categoryId.HasValue)
                {
                    transaction.CategoryId = categoryId;
                }

                break;
            }
            case RuleActionType.AddTag:
            {
                var tags = transaction.Tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(rule.ActionValue))
                {
                    tags.Add(rule.ActionValue.Trim());
                }

                transaction.Tags = tags.ToArray();
                break;
            }
            case RuleActionType.TriggerAlert:
            {
                var marker = $"[Rule Alert: {rule.ActionValue}]";
                var currentNote = transaction.Note?.Trim() ?? string.Empty;
                if (!currentNote.Contains(marker, StringComparison.OrdinalIgnoreCase))
                {
                    transaction.Note = string.IsNullOrWhiteSpace(currentNote) ? marker : $"{currentNote} {marker}";
                }

                break;
            }
        }
    }

    private static bool ConditionMatches(Rule rule, Transaction transaction, string? categoryName)
    {
        return rule.ConditionField switch
        {
            RuleField.Merchant => MatchString(transaction.Merchant, rule.ConditionOperator, rule.ConditionValue),
            RuleField.Category => MatchString(categoryName, rule.ConditionOperator, rule.ConditionValue) ||
                                  MatchString(transaction.CategoryId?.ToString(), rule.ConditionOperator, rule.ConditionValue),
            RuleField.TransactionType => MatchString(transaction.Type.ToString(), rule.ConditionOperator, rule.ConditionValue),
            RuleField.Amount => MatchNumber(transaction.Amount, rule.ConditionOperator, rule.ConditionValue),
            _ => false
        };
    }

    private static bool MatchString(string? source, RuleOperator ruleOperator, string expected)
    {
        var left = source?.Trim() ?? string.Empty;
        var right = expected.Trim();
        return ruleOperator switch
        {
            RuleOperator.Equals => left.Equals(right, StringComparison.OrdinalIgnoreCase),
            RuleOperator.Contains => left.Contains(right, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool MatchNumber(decimal source, RuleOperator ruleOperator, string expected)
    {
        if (!decimal.TryParse(expected, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        return ruleOperator switch
        {
            RuleOperator.Equals => source == value,
            RuleOperator.GreaterThan => source > value,
            RuleOperator.LessThan => source < value,
            _ => false
        };
    }

    private static RuleResponse ToResponse(Rule item) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            ConditionField = item.ConditionField,
            ConditionOperator = item.ConditionOperator,
            ConditionValue = item.ConditionValue,
            ActionType = item.ActionType,
            ActionValue = item.ActionValue,
            IsActive = item.IsActive,
            Priority = item.Priority
        };
}
