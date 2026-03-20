using Microsoft.EntityFrameworkCore;
using PersonalFinanceTracker.Application.Abstractions;
using PersonalFinanceTracker.Application.DTOs;
using PersonalFinanceTracker.Application.DTOs.Transactions;
using PersonalFinanceTracker.Application.Exceptions;
using PersonalFinanceTracker.Domain.Entities;
using PersonalFinanceTracker.Domain.Enums;
using PersonalFinanceTracker.Infrastructure.Persistence;

namespace PersonalFinanceTracker.Infrastructure.Services;

public sealed class TransactionService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IBalanceService balanceService,
    IAuditService auditService) : ITransactionService
{
    public async Task<PagedResult<TransactionResponse>> GetAllAsync(TransactionQueryRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var query = ApplyFilters(dbContext.Transactions
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.DestinationAccount)
            .Include(x => x.Category)
            .Where(x => x.UserId == userId), request);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TransactionResponse>
        {
            Items = items.Select(x => x.ToResponse()).ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<TransactionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var transaction = await QueryTransactions(userId)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Transaction not found.");

        return transaction.ToResponse();
    }

    public async Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        await ValidateOwnershipAsync(userId, request.AccountId, request.DestinationAccountId, request.CategoryId, request.Type, cancellationToken);
        ValidationGuard.AgainstNonPositiveAmount(request.Amount);
        ValidationGuard.AgainstInvalidTransaction(request.Type, request.CategoryId, request.DestinationAccountId, request.AccountId);

        var transaction = new Transaction
        {
            UserId = userId,
            AccountId = request.AccountId,
            DestinationAccountId = request.Type == TransactionType.Transfer ? request.DestinationAccountId : null,
            CategoryId = request.Type == TransactionType.Transfer ? null : request.CategoryId,
            Type = request.Type,
            Amount = request.Amount,
            TransactionDate = request.Date,
            Note = request.Note?.Trim(),
            Merchant = request.Merchant?.Trim(),
            PaymentMethod = request.PaymentMethod?.Trim(),
            Tags = request.Tags.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray(),
            RecurringTransactionId = request.RecurringTransactionId
        };

        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Transactions.AddAsync(transaction, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await balanceService.RecalculateForUserAsync(userId, cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        await auditService.WriteAsync(userId, "transaction_created", nameof(Transaction), transaction.Id, new { transaction.Type, transaction.Amount }, cancellationToken);

        return await GetByIdAsync(transaction.Id, cancellationToken);
    }

    public async Task<TransactionResponse> UpdateAsync(Guid id, UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var transaction = await dbContext.Transactions.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Transaction not found.");

        await ValidateOwnershipAsync(userId, request.AccountId, request.DestinationAccountId, request.CategoryId, request.Type, cancellationToken);
        ValidationGuard.AgainstNonPositiveAmount(request.Amount);
        ValidationGuard.AgainstInvalidTransaction(request.Type, request.CategoryId, request.DestinationAccountId, request.AccountId);

        transaction.AccountId = request.AccountId;
        transaction.DestinationAccountId = request.Type == TransactionType.Transfer ? request.DestinationAccountId : null;
        transaction.CategoryId = request.Type == TransactionType.Transfer ? null : request.CategoryId;
        transaction.Type = request.Type;
        transaction.Amount = request.Amount;
        transaction.TransactionDate = request.Date;
        transaction.Note = request.Note?.Trim();
        transaction.Merchant = request.Merchant?.Trim();
        transaction.PaymentMethod = request.PaymentMethod?.Trim();
        transaction.Tags = request.Tags.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray();

        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await balanceService.RecalculateForUserAsync(userId, cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        await auditService.WriteAsync(userId, "transaction_updated", nameof(Transaction), transaction.Id, new { transaction.Type, transaction.Amount }, cancellationToken);

        return await GetByIdAsync(transaction.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var transaction = await dbContext.Transactions.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Transaction not found.");

        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Transactions.Remove(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
        await balanceService.RecalculateForUserAsync(userId, cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        await auditService.WriteAsync(userId, "transaction_deleted", nameof(Transaction), transaction.Id, new { transaction.Type, transaction.Amount }, cancellationToken);
    }

    internal static IQueryable<Transaction> ApplyFilters(IQueryable<Transaction> query, TransactionQueryRequest request)
    {
        if (request.StartDate.HasValue)
        {
            query = query.Where(x => x.TransactionDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(x => x.TransactionDate <= request.EndDate.Value);
        }

        if (request.AccountId.HasValue)
        {
            query = query.Where(x => x.AccountId == request.AccountId.Value || x.DestinationAccountId == request.AccountId.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);
        }

        if (request.MinAmount.HasValue)
        {
            query = query.Where(x => x.Amount >= request.MinAmount.Value);
        }

        if (request.MaxAmount.HasValue)
        {
            query = query.Where(x => x.Amount <= request.MaxAmount.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(x => x.Type == request.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.Merchant != null && x.Merchant.ToLower().Contains(search)) ||
                (x.Note != null && x.Note.ToLower().Contains(search)));
        }

        return query;
    }

    private IQueryable<Transaction> QueryTransactions(Guid userId) =>
        dbContext.Transactions
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.DestinationAccount)
            .Include(x => x.Category)
            .Where(x => x.UserId == userId);

    private async Task ValidateOwnershipAsync(
        Guid userId,
        Guid accountId,
        Guid? destinationAccountId,
        Guid? categoryId,
        TransactionType type,
        CancellationToken cancellationToken)
    {
        var accountExists = await dbContext.Accounts.AnyAsync(x => x.Id == accountId && x.UserId == userId, cancellationToken);
        if (!accountExists)
        {
            throw new ValidationException("Selected account does not exist.");
        }

        if (destinationAccountId.HasValue)
        {
            var destinationExists = await dbContext.Accounts.AnyAsync(x => x.Id == destinationAccountId.Value && x.UserId == userId, cancellationToken);
            if (!destinationExists)
            {
                throw new ValidationException("Selected destination account does not exist.");
            }
        }

        if (type != TransactionType.Transfer && categoryId.HasValue)
        {
            var categoryExists = await dbContext.Categories.AnyAsync(x => x.Id == categoryId.Value && x.UserId == userId, cancellationToken);
            if (!categoryExists)
            {
                throw new ValidationException("Selected category does not exist.");
            }
        }
    }
}
