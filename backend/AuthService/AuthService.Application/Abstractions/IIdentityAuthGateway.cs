namespace AuthService.Application.Abstractions;

public interface IIdentityAuthGateway
{
    Task<RegisterGatewayResult> RegisterAsync(string email, string password, string role, CancellationToken cancellationToken = default);

    Task<ConfirmEmailGatewayResult> ConfirmEmailAsync(string userId, string encodedToken, CancellationToken cancellationToken = default);

    Task<LoginGatewayResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task SaveRefreshTokenAsync(string userId, string refreshToken, DateTime expiresAtUtc, CancellationToken cancellationToken = default);

    Task<RefreshTokenValidationResult> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(string userId, CancellationToken cancellationToken = default);

    Task<ForgotPasswordGatewayResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);

    Task<ResetPasswordGatewayResult> ResetPasswordAsync(string email, string encodedToken, string newPassword, CancellationToken cancellationToken = default);

    Task<ChangePasswordGatewayResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    Task<ChangeEmailGatewayResult> ChangeEmailAsync(string userId, string newEmail, string password, CancellationToken cancellationToken = default);

    Task<ExternalLoginGatewayResult> LoginOrCreateExternalAsync(
        string provider,
        string providerUserId,
        string? email,
        string role,
        CancellationToken cancellationToken = default);
}

public sealed record RegisterGatewayResult(bool IsSuccess, string? UserId, string? EncodedToken, IReadOnlyCollection<string> Errors);

public sealed record ConfirmEmailGatewayResult(bool IsSuccess, IReadOnlyCollection<string> Errors);

public enum LoginStatus
{
    Success,
    InvalidCredentials,
    EmailNotConfirmed,
    LockedOut
}

public sealed record LoginGatewayResult(LoginStatus Status, string? UserId = null, string? Email = null, string? Role = null);

public sealed record RefreshTokenValidationResult(bool IsValid, string? UserId = null, string? Email = null, string? Role = null);

public sealed record ForgotPasswordGatewayResult(bool CanSendEmail, string? UserId, string? Email, string? EncodedToken);

public sealed record ResetPasswordGatewayResult(bool IsSuccess, IReadOnlyCollection<string> Errors);

public sealed record ChangePasswordGatewayResult(bool IsSuccess, IReadOnlyCollection<string> Errors);

public sealed record ChangeEmailGatewayResult(bool IsSuccess, IReadOnlyCollection<string> Errors);

public sealed record ExternalLoginGatewayResult(
    bool IsSuccess,
    string? UserId,
    string? Email,
    string? Role,
    IReadOnlyCollection<string>? Errors = null);
