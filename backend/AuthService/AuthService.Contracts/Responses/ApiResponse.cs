namespace AuthService.Contracts.Responses;

public sealed record ApiResponse(bool Success, string Message, IReadOnlyCollection<string>? Errors = null);
