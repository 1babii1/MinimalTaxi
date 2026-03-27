namespace AuthService.Application.Abstractions;

public interface IAuthService
{
    Task<AuthOperationResult> RegisterAsync(
        string email,
        string password,
        string role,
        RegistrationProfileData? profileData = null,
        CancellationToken cancellationToken = default);
    Task<AuthOperationResult> ConfirmEmailAsync(string userId, string token, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> ChangeEmailAsync(string userId, string newEmail, string password, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> LoginExternalAsync(string provider, string providerUserId, string? email, string role, CancellationToken cancellationToken = default);
}

public sealed record RegistrationProfileData(
    string? Name,
    string? Phone,
    string Role,
    RegistrationAddressData? Address,
    RegistrationCarInfoData? CarInfo);

public sealed record RegistrationAddressData(
    string? City,
    string? Street,
    string? House,
    string? Apartment);

public sealed record RegistrationCarInfoData(
    string? Brand,
    string? Model,
    string? Color,
    string? PlateNumber);

public sealed record AuthOperationResult(
    bool IsSuccess,
    string Message,
    IReadOnlyCollection<string>? Errors = null,
    string? AccessToken = null,
    string? RefreshToken = null)
{
    public static AuthOperationResult Success(string message) => new(true, message);

    public static AuthOperationResult Success(string message, string accessToken, string refreshToken) =>
        new(true, message, null, accessToken, refreshToken);

    public static AuthOperationResult Failure(string message, IReadOnlyCollection<string>? errors = null) =>
        new(false, message, errors, null, null);
}
