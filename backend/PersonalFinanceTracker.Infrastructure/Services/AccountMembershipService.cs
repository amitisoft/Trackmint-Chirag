using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs.Accounts;
using PersonalFinanceTracker.Application.Exceptions;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Services;

public sealed class AccountMembershipService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IAccountAccessService accountAccessService,
    IAuditService auditService) : IAccountMembershipService
{
    public async Task<IReadOnlyCollection<AccountMemberResponse>> GetMembersAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        await accountAccessService.EnsureCanViewAccountAsync(userId, accountId, cancellationToken);

        var account = await dbContext.Accounts
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken)
            ?? throw new NotFoundException("Account not found.");

        var owner = new AccountMemberResponse
        {
            UserId = account.UserId,
            Email = account.User?.Email ?? string.Empty,
            DisplayName = account.User?.DisplayName ?? "Owner",
            Role = AccountMemberRole.Owner,
            IsOwner = true
        };

        var members = await dbContext.AccountMembers
            .Include(x => x.User)
            .Where(x => x.AccountId == accountId)
            .OrderBy(x => x.User!.DisplayName)
            .Select(x => new AccountMemberResponse
            {
                UserId = x.UserId,
                Email = x.User!.Email,
                DisplayName = x.User!.DisplayName,
                Role = x.Role,
                IsOwner = false
            })
            .ToArrayAsync(cancellationToken);

        return [owner, .. members];
    }

    public async Task InviteAsync(Guid accountId, InviteAccountMemberRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetUserId();
        await accountAccessService.EnsureCanManageAccountAsync(currentUserId, accountId, cancellationToken);

        ValidationGuard.AgainstBlank(request.Email, "Email");

        var invitee = await dbContext.Users.SingleOrDefaultAsync(
            x => x.Email.ToLower() == request.Email.Trim().ToLower(),
            cancellationToken);

        if (invitee is null)
        {
            throw new ValidationException("Invited user must register first with this email.");
        }

        var account = await dbContext.Accounts
            .SingleAsync(x => x.Id == accountId, cancellationToken);

        if (invitee.Id == account.UserId)
        {
            throw new ValidationException("Owner is already part of this account.");
        }

        var existing = await dbContext.AccountMembers
            .SingleOrDefaultAsync(x => x.AccountId == accountId && x.UserId == invitee.Id, cancellationToken);

        var targetRole = request.Role == AccountMemberRole.Owner ? AccountMemberRole.Editor : request.Role;

        if (existing is null)
        {
            var membership = new AccountMember
            {
                AccountId = accountId,
                UserId = invitee.Id,
                Role = targetRole
            };
            await dbContext.AccountMembers.AddAsync(membership, cancellationToken);
        }
        else
        {
            existing.Role = targetRole;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            currentUserId,
            "account_member_invited",
            nameof(AccountMember),
            accountId,
            new { request.Email, Role = targetRole.ToString() },
            cancellationToken);
    }

    public async Task UpdateMemberRoleAsync(Guid accountId, Guid memberUserId, UpdateAccountMemberRoleRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetUserId();
        await accountAccessService.EnsureCanManageAccountAsync(currentUserId, accountId, cancellationToken);

        var account = await dbContext.Accounts
            .SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken)
            ?? throw new NotFoundException("Account not found.");

        if (memberUserId == account.UserId)
        {
            throw new ValidationException("Owner role cannot be changed.");
        }

        var membership = await dbContext.AccountMembers
            .SingleOrDefaultAsync(x => x.AccountId == accountId && x.UserId == memberUserId, cancellationToken)
            ?? throw new NotFoundException("Member not found.");

        membership.Role = request.Role == AccountMemberRole.Owner ? AccountMemberRole.Editor : request.Role;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            currentUserId,
            "account_member_role_updated",
            nameof(AccountMember),
            accountId,
            new { memberUserId, membership.Role },
            cancellationToken);
    }
}
