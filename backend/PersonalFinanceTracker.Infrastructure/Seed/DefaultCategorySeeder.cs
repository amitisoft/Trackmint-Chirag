using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Seed;

public sealed class DefaultCategorySeeder(ApplicationDbContext dbContext) : IDefaultCategorySeeder
{
    private static readonly (string Name, CategoryType Type, string Color, string Icon)[] CategoryTemplates =
    [
        ("Food", CategoryType.Expense, "#ef4444", "utensils"),
        ("Rent", CategoryType.Expense, "#f97316", "home"),
        ("Utilities", CategoryType.Expense, "#f59e0b", "bolt"),
        ("Transport", CategoryType.Expense, "#8b5cf6", "car"),
        ("Entertainment", CategoryType.Expense, "#ec4899", "film"),
        ("Shopping", CategoryType.Expense, "#06b6d4", "shopping-bag"),
        ("Health", CategoryType.Expense, "#10b981", "heart"),
        ("Education", CategoryType.Expense, "#3b82f6", "book-open"),
        ("Travel", CategoryType.Expense, "#0ea5e9", "plane"),
        ("Subscriptions", CategoryType.Expense, "#6366f1", "repeat"),
        ("Miscellaneous", CategoryType.Expense, "#64748b", "layers"),
        ("Salary", CategoryType.Income, "#16a34a", "briefcase"),
        ("Freelance", CategoryType.Income, "#22c55e", "laptop"),
        ("Bonus", CategoryType.Income, "#84cc16", "sparkles"),
        ("Investment", CategoryType.Income, "#14b8a6", "trending-up"),
        ("Gift", CategoryType.Income, "#eab308", "gift"),
        ("Refund", CategoryType.Income, "#38bdf8", "undo"),
        ("Other", CategoryType.Income, "#475569", "wallet")
    ];

    public async Task SeedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var categories = CategoryTemplates.Select(template => new Category
        {
            UserId = userId,
            Name = template.Name,
            Type = template.Type,
            Color = template.Color,
            Icon = template.Icon
        });

        await dbContext.Categories.AddRangeAsync(categories, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
