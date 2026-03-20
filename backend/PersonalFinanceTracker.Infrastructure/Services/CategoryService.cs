using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Categories;
using PersonalFinanceTracker.Application.Exceptions;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Services;

public sealed class CategoryService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IAuditService auditService) : ICategoryService
{
    public async Task<IReadOnlyCollection<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var categories = await dbContext.Categories
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(x => x.ToResponse()).ToArray();
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        ValidationGuard.AgainstBlank(request.Name, "Category name");

        var exists = await dbContext.Categories.AnyAsync(
            x => x.UserId == userId && x.Type == request.Type && x.Name.ToLower() == request.Name.Trim().ToLower(),
            cancellationToken);

        if (exists)
        {
            throw new ValidationException("Category already exists.");
        }

        var category = new Category
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Type = request.Type,
            Color = request.Color,
            Icon = request.Icon
        };

        await dbContext.Categories.AddAsync(category, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(userId, "category_created", nameof(Category), category.Id, new { category.Name }, cancellationToken);

        return category.ToResponse();
    }

    public async Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var category = await dbContext.Categories.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Category not found.");

        ValidationGuard.AgainstBlank(request.Name, "Category name");

        category.Name = request.Name.Trim();
        category.Color = request.Color;
        category.Icon = request.Icon;
        category.IsArchived = request.IsArchived;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(userId, "category_updated", nameof(Category), category.Id, new { category.Name }, cancellationToken);

        return category.ToResponse();
    }

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var category = await dbContext.Categories.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Category not found.");

        category.IsArchived = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(userId, "category_archived", nameof(Category), category.Id, new { category.Name }, cancellationToken);
    }
}
