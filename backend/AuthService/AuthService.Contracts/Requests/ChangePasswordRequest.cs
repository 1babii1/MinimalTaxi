namespace AuthService.Contracts.Requests;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);