namespace PersonalFinanceTracker.Application.Exceptions;

public sealed class ValidationException(string message) : AppException(message, 400);
