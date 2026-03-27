namespace AuthService.Contracts.Requests;

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
