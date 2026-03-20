using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Categories;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<CategoryResponse>> GetAll(CancellationToken cancellationToken) =>
        categoryService.GetAllAsync(cancellationToken);

    [HttpPost]
    public Task<CategoryResponse> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken) =>
        categoryService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<CategoryResponse> Update(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken) =>
        categoryService.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await categoryService.ArchiveAsync(id, cancellationToken);
        return NoContent();
    }
}
