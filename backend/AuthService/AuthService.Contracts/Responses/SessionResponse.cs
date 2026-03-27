namespace AuthService.Contracts.Responses;

public sealed record SessionResponse(bool IsAuthenticated, string? UserId, string? Email, string? Role);
