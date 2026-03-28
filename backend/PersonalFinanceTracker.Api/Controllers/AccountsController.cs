using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Accounts;

namespace PersonalFinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts")]
public sealed class AccountsController(
    IAccountService accountService,
    IAccountMembershipService accountMembershipService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<AccountResponse>> GetAll(CancellationToken cancellationToken) =>
        accountService.GetAllAsync(cancellationToken);

    [HttpPost]
    public Task<AccountResponse> Create([FromBody] CreateAccountRequest request, CancellationToken cancellationToken) =>
        accountService.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<AccountResponse> Update(Guid id, [FromBody] UpdateAccountRequest request, CancellationToken cancellationToken) =>
        accountService.UpdateAsync(id, request, cancellationToken);

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferFundsRequest request, CancellationToken cancellationToken)
    {
        await accountService.TransferAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/members")]
    public Task<IReadOnlyCollection<AccountMemberResponse>> GetMembers(Guid id, CancellationToken cancellationToken) =>
        accountMembershipService.GetMembersAsync(id, cancellationToken);

    [HttpPost("{id:guid}/invite")]
    public async Task<IActionResult> Invite(Guid id, [FromBody] InviteAccountMemberRequest request, CancellationToken cancellationToken)
    {
        await accountMembershipService.InviteAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> UpdateMemberRole(
        Guid id,
        Guid userId,
        [FromBody] UpdateAccountMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        await accountMembershipService.UpdateMemberRoleAsync(id, userId, request, cancellationToken);
        return NoContent();
    }
}
