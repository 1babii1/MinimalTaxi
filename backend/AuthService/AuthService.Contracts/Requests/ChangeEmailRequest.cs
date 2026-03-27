namespace AuthService.Contracts.Requests;

public sealed record ChangeEmailRequest(string NewEmail, string Password);