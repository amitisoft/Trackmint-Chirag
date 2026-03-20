using System.Text.RegularExpressions;
using PersonalFinanceTracker.Application.Exceptions;
using PersonalFinanceTracker.Domain.Enums;

namespace PersonalFinanceTracker.Infrastructure.Services;

internal static partial class ValidationGuard
{
    public static void AgainstInvalidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ValidationException("Password must be at least 8 characters.");
        }

        if (!PasswordRegex().IsMatch(password))
        {
            throw new ValidationException("Password must include uppercase, lowercase, and a number.");
        }
    }

    public static void AgainstNonPositiveAmount(decimal amount, string fieldName = "Amount")
    {
        if (amount <= 0)
        {
            throw new ValidationException($"{fieldName} must be greater than zero.");
        }
    }

    public static void AgainstNegativeAmount(decimal amount, string fieldName = "Amount")
    {
        if (amount < 0)
        {
            throw new ValidationException($"{fieldName} cannot be negative.");
        }
    }

    public static void AgainstBlank(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"{fieldName} is required.");
        }
    }

    public static void AgainstInvalidTransaction(TransactionType type, Guid? categoryId, Guid? destinationAccountId, Guid accountId)
    {
        if (type == TransactionType.Transfer)
        {
            if (!destinationAccountId.HasValue)
            {
                throw new ValidationException("Transfer requires a destination account.");
            }

            if (destinationAccountId.Value == accountId)
            {
                throw new ValidationException("Transfer destination must be different from source account.");
            }
        }
        else if (!categoryId.HasValue)
        {
            throw new ValidationException("Category is required for income and expense transactions.");
        }
    }

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", RegexOptions.Compiled)]
    private static partial Regex PasswordRegex();
}
