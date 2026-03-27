namespace AuthService.Contracts.Requests;

public sealed record ConfirmEmailRequest(string UserId, string Token);
