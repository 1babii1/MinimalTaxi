using AuthService.Application.Abstractions;
using AuthService.Domain.ValueObjects;

namespace AuthService.Application.Services;

public class AuthService(
    IIdentityAuthGateway identityAuthGateway,
    IEmailSender emailSender,
    IFrontendUrlProvider frontendUrlProvider,
    IJwtTokenProvider jwtTokenProvider,
    IProfileSyncGateway profileSyncGateway) : IAuthService
{
    private static string NormalizeRole(string? rawRole)
    {
        if (string.Equals(rawRole, "driver", StringComparison.OrdinalIgnoreCase))
            return "Driver";

        return "Passenger";
    }

    public async Task<AuthOperationResult> RegisterAsync(
        string email,
        string password,
        string role,
        RegistrationProfileData? profileData = null,
        CancellationToken cancellationToken = default)
    {
        if (!Email.TryCreate(email, out var parsedEmail, out var emailError))
            return AuthOperationResult.Failure(emailError!);

        if (!Password.TryCreate(password, out var parsedPassword, out var passwordError))
            return AuthOperationResult.Failure(passwordError!);

        var normalizedRole = NormalizeRole(role);
        var registerResult = await identityAuthGateway.RegisterAsync(parsedEmail!.Value, parsedPassword!.Value, normalizedRole, cancellationToken);

        if (!registerResult.IsSuccess)
            return AuthOperationResult.Failure("Registration failed.", registerResult.Errors);

        if (profileData is not null
            && !string.IsNullOrWhiteSpace(registerResult.UserId)
            && !string.IsNullOrWhiteSpace(profileData.Name))
        {
            try
            {
                await profileSyncGateway.SyncRegistrationProfileAsync(
                    registerResult.UserId!,
                    profileData with { Role = normalizedRole },
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _ = exception;
            }
        }

        var confirmUrl = frontendUrlProvider.BuildConfirmEmailUrl(registerResult.UserId!, registerResult.EncodedToken!);

        await emailSender.SendAsync(
            parsedEmail.Value,
            "Подтверждение почты MinimalTaxi",
            BuildActionEmailHtml(
                title: "Подтвердите адрес электронной почты",
                description: "Нажмите кнопку ниже, чтобы завершить регистрацию в MinimalTaxi.",
                actionText: "Подтвердить почту",
                actionUrl: confirmUrl),
            cancellationToken);

        return AuthOperationResult.Success("Registration successful. Check your email for confirmation link.");
    }

    public async Task<AuthOperationResult> ConfirmEmailAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return AuthOperationResult.Failure("User id is required.");

        if (string.IsNullOrWhiteSpace(token))
            return AuthOperationResult.Failure("Token is required.");

        var result = await identityAuthGateway.ConfirmEmailAsync(userId.Trim(), token.Trim(), cancellationToken);

        return result.IsSuccess
            ? AuthOperationResult.Success("Email confirmed successfully.")
            : AuthOperationResult.Failure("Email confirmation failed.", result.Errors);
    }

    public async Task<AuthOperationResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (!Email.TryCreate(email, out var parsedEmail, out var emailError))
            return AuthOperationResult.Failure(emailError!);

        if (string.IsNullOrWhiteSpace(password))
            return AuthOperationResult.Failure("Password is required.");

        var loginResult = await identityAuthGateway.LoginAsync(parsedEmail!.Value, password, cancellationToken);

        if (loginResult.Status == LoginStatus.Success)
        {
            var accessToken = jwtTokenProvider.CreateAccessToken(
                loginResult.UserId!,
                loginResult.Email!,
                loginResult.Role ?? "Passenger");
            var refreshToken = jwtTokenProvider.CreateRefreshToken(loginResult.UserId!);
            var refreshExpiresAtUtc = DateTime.UtcNow.AddDays(30);

            await identityAuthGateway.SaveRefreshTokenAsync(
                loginResult.UserId!,
                refreshToken,
                refreshExpiresAtUtc,
                cancellationToken);

            return AuthOperationResult.Success("Login successful.", accessToken, refreshToken);
        }

        return loginResult.Status switch
        {
            LoginStatus.EmailNotConfirmed => AuthOperationResult.Failure("Please confirm your email first."),
            LoginStatus.LockedOut => AuthOperationResult.Failure("Account is temporarily locked."),
            _ => AuthOperationResult.Failure("Invalid email or password.")
        };
    }

    public async Task<AuthOperationResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return AuthOperationResult.Failure("Refresh token is required.");

        var validation = await identityAuthGateway.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
        if (!validation.IsValid)
            return AuthOperationResult.Failure("Refresh token is invalid or expired.");

        var accessToken = jwtTokenProvider.CreateAccessToken(
            validation.UserId!,
            validation.Email!,
            validation.Role ?? "Passenger");
        var newRefreshToken = jwtTokenProvider.CreateRefreshToken(validation.UserId!);
        var refreshExpiresAtUtc = DateTime.UtcNow.AddDays(30);

        await identityAuthGateway.SaveRefreshTokenAsync(
            validation.UserId!,
            newRefreshToken,
            refreshExpiresAtUtc,
            cancellationToken);

        return AuthOperationResult.Success("Session refreshed.", accessToken, newRefreshToken);
    }

    public async Task<AuthOperationResult> LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return AuthOperationResult.Success("Logged out.");

        var userId = refreshToken.Split('.', 2).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
            return AuthOperationResult.Success("Logged out.");

        await identityAuthGateway.RevokeRefreshTokenAsync(userId, cancellationToken);
        return AuthOperationResult.Success("Logged out.");
    }

    public async Task<AuthOperationResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        if (!Email.TryCreate(email, out var parsedEmail, out _))
            return AuthOperationResult.Success("If this email exists and is confirmed, a password reset link has been sent.");

        var forgotResult = await identityAuthGateway.ForgotPasswordAsync(parsedEmail!.Value, cancellationToken);

        if (forgotResult.CanSendEmail)
        {
            var resetUrl = frontendUrlProvider.BuildResetPasswordUrl(forgotResult.Email!, forgotResult.EncodedToken!);

            await emailSender.SendAsync(
                forgotResult.Email!,
                "Сброс пароля MinimalTaxi",
                BuildActionEmailHtml(
                    title: "Запрос на смену пароля",
                    description: "Нажмите кнопку ниже, чтобы задать новый пароль.",
                    actionText: "Сбросить пароль",
                    actionUrl: resetUrl),
                cancellationToken);
        }

        return AuthOperationResult.Success("If this email exists and is confirmed, a password reset link has been sent.");
    }

    public async Task<AuthOperationResult> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        if (!Email.TryCreate(email, out var parsedEmail, out var emailError))
            return AuthOperationResult.Failure(emailError!);

        if (string.IsNullOrWhiteSpace(token))
            return AuthOperationResult.Failure("Token is required.");

        if (!Password.TryCreate(newPassword, out var parsedPassword, out var passwordError))
            return AuthOperationResult.Failure(passwordError!);

        var result = await identityAuthGateway.ResetPasswordAsync(parsedEmail!.Value, token.Trim(), parsedPassword!.Value, cancellationToken);

        return result.IsSuccess
            ? AuthOperationResult.Success("Password was reset successfully.")
            : AuthOperationResult.Failure("Password reset failed.", result.Errors);
    }

    public async Task<AuthOperationResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return AuthOperationResult.Failure("Unauthorized.");

        if (string.IsNullOrWhiteSpace(currentPassword))
            return AuthOperationResult.Failure("Current password is required.");

        if (!Password.TryCreate(newPassword, out var parsedPassword, out var passwordError))
            return AuthOperationResult.Failure(passwordError!);

        var result = await identityAuthGateway.ChangePasswordAsync(
            userId,
            currentPassword,
            parsedPassword!.Value,
            cancellationToken);

        return result.IsSuccess
            ? AuthOperationResult.Success("Password changed successfully.")
            : AuthOperationResult.Failure("Failed to change password.", result.Errors);
    }

    public async Task<AuthOperationResult> ChangeEmailAsync(string userId, string newEmail, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return AuthOperationResult.Failure("Unauthorized.");

        if (!Email.TryCreate(newEmail, out var parsedEmail, out var emailError))
            return AuthOperationResult.Failure(emailError!);

        if (string.IsNullOrWhiteSpace(password))
            return AuthOperationResult.Failure("Password is required.");

        var result = await identityAuthGateway.ChangeEmailAsync(
            userId,
            parsedEmail!.Value,
            password,
            cancellationToken);

        return result.IsSuccess
            ? AuthOperationResult.Success("Email changed successfully. Please sign in again.")
            : AuthOperationResult.Failure("Failed to change email.", result.Errors);
    }

    public async Task<AuthOperationResult> LoginExternalAsync(
        string provider,
        string providerUserId,
        string? email,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return AuthOperationResult.Failure("Provider is required.");

        if (string.IsNullOrWhiteSpace(providerUserId))
            return AuthOperationResult.Failure("External user id is required.");

        var normalizedRole = NormalizeRole(role);

        var gatewayResult = await identityAuthGateway.LoginOrCreateExternalAsync(
            provider,
            providerUserId,
            email,
            normalizedRole,
            cancellationToken);

        if (!gatewayResult.IsSuccess || string.IsNullOrWhiteSpace(gatewayResult.UserId) || string.IsNullOrWhiteSpace(gatewayResult.Email))
            return AuthOperationResult.Failure("External login failed.", gatewayResult.Errors);

        var resolvedRole = gatewayResult.Role ?? normalizedRole;
        var accessToken = jwtTokenProvider.CreateAccessToken(
            gatewayResult.UserId,
            gatewayResult.Email,
            resolvedRole);
        var refreshToken = jwtTokenProvider.CreateRefreshToken(gatewayResult.UserId);
        var refreshExpiresAtUtc = DateTime.UtcNow.AddDays(30);

        await identityAuthGateway.SaveRefreshTokenAsync(
            gatewayResult.UserId,
            refreshToken,
            refreshExpiresAtUtc,
            cancellationToken);

        return AuthOperationResult.Success("External login successful.", accessToken, refreshToken);
    }

    private static string BuildActionEmailHtml(
            string title,
            string description,
            string actionText,
            string actionUrl)
    {
        return $"""
<!doctype html>
<html lang='ru'>
    <body style='margin:0;padding:0;background:#f5f7fb;font-family:Segoe UI,Arial,sans-serif;color:#1f2937;'>
        <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='padding:24px 12px;'>
            <tr>
                <td align='center'>
                    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='max-width:560px;background:#ffffff;border:1px solid #e5e7eb;border-radius:14px;padding:24px;'>
                        <tr>
                            <td>
                                <h2 style='margin:0 0 12px 0;font-size:20px;line-height:1.3;'>{title}</h2>
                                <p style='margin:0 0 20px 0;font-size:14px;line-height:1.6;color:#4b5563;'>{description}</p>
                                <a href='{actionUrl}' style='display:inline-block;background:#111827;color:#ffffff;text-decoration:none;font-size:14px;font-weight:600;padding:11px 18px;border-radius:10px;'>{actionText}</a>
                                <p style='margin:20px 0 0 0;font-size:12px;line-height:1.5;color:#6b7280;'>
                                    Если кнопка не работает, откройте письмо в браузере и повторите попытку позже.
                                </p>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </body>
</html>
""";
    }
}
