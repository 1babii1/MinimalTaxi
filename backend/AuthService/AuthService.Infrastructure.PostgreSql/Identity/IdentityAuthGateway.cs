using System.Text;
using System.Security.Cryptography;
using AuthService.Application.Abstractions;
using AuthService.Infrastructure.PostgreSql.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace AuthService.Infrastructure.PostgreSql.Identity;

public class IdentityAuthGateway(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : IIdentityAuthGateway
{
    private const string RefreshLoginProvider = "MinimalTaxiAuth";
    private const string RefreshTokenName = "RefreshToken";
    private const string RefreshTokenExpiryName = "RefreshTokenExpiresAt";

    public async Task<RegisterGatewayResult> RegisterAsync(string email, string password, string role, CancellationToken cancellationToken = default)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return new RegisterGatewayResult(false, null, null, ["User with this email already exists."]);

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            EmailConfirmed = false
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            return new RegisterGatewayResult(false, null, null, createResult.Errors.Select(x => x.Description).ToArray());

        var normalizedRole = string.Equals(role, "Driver", StringComparison.OrdinalIgnoreCase)
            ? "Driver"
            : "Passenger";

        if (!await roleManager.RoleExistsAsync(normalizedRole))
        {
            var roleCreateResult = await roleManager.CreateAsync(new IdentityRole(normalizedRole));
            if (!roleCreateResult.Succeeded)
                return new RegisterGatewayResult(false, null, null, roleCreateResult.Errors.Select(x => x.Description).ToArray());
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, normalizedRole);
        if (!addRoleResult.Succeeded)
            return new RegisterGatewayResult(false, null, null, addRoleResult.Errors.Select(x => x.Description).ToArray());

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        return new RegisterGatewayResult(true, user.Id, encodedToken, Array.Empty<string>());
    }

    public async Task<ConfirmEmailGatewayResult> ConfirmEmailAsync(string userId, string encodedToken, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return new ConfirmEmailGatewayResult(false, ["Invalid confirmation request."]);

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
        catch
        {
            return new ConfirmEmailGatewayResult(false, ["Invalid confirmation token format."]);
        }

        var result = await userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded
            ? new ConfirmEmailGatewayResult(true, Array.Empty<string>())
            : new ConfirmEmailGatewayResult(false, result.Errors.Select(x => x.Description).ToArray());
    }

    public async Task<LoginGatewayResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return new LoginGatewayResult(LoginStatus.InvalidCredentials);

        if (!user.EmailConfirmed)
            return new LoginGatewayResult(LoginStatus.EmailNotConfirmed);

        if (await userManager.IsLockedOutAsync(user))
            return new LoginGatewayResult(LoginStatus.LockedOut);

        var isValidPassword = await userManager.CheckPasswordAsync(user, password);
        if (isValidPassword)
        {
            await userManager.ResetAccessFailedCountAsync(user);
            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Passenger";

            return new LoginGatewayResult(LoginStatus.Success, user.Id, user.Email, role);
        }

        await userManager.AccessFailedAsync(user);

        if (await userManager.IsLockedOutAsync(user))
            return new LoginGatewayResult(LoginStatus.LockedOut);

        return new LoginGatewayResult(LoginStatus.InvalidCredentials);
    }

    public async Task<ForgotPasswordGatewayResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null || !user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
            return new ForgotPasswordGatewayResult(false, null, null, null);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        return new ForgotPasswordGatewayResult(true, user.Id, user.Email, encodedToken);
    }

    public async Task SaveRefreshTokenAsync(string userId, string refreshToken, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return;

        await userManager.SetAuthenticationTokenAsync(user, RefreshLoginProvider, RefreshTokenName, refreshToken);
        await userManager.SetAuthenticationTokenAsync(
            user,
            RefreshLoginProvider,
            RefreshTokenExpiryName,
            expiresAtUtc.ToString("O"));
    }

    public async Task<RefreshTokenValidationResult> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var userId = refreshToken.Split('.', 2).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
            return new RefreshTokenValidationResult(false);

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
            return new RefreshTokenValidationResult(false);

        var storedToken = await userManager.GetAuthenticationTokenAsync(user, RefreshLoginProvider, RefreshTokenName);
        var expiryRaw = await userManager.GetAuthenticationTokenAsync(user, RefreshLoginProvider, RefreshTokenExpiryName);

        if (string.IsNullOrWhiteSpace(storedToken) || !string.Equals(storedToken, refreshToken, StringComparison.Ordinal))
            return new RefreshTokenValidationResult(false);

        if (!DateTime.TryParse(expiryRaw, out var expiresAtUtc) || expiresAtUtc <= DateTime.UtcNow)
            return new RefreshTokenValidationResult(false);

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Passenger";

        return new RefreshTokenValidationResult(true, user.Id, user.Email, role);
    }

    public async Task RevokeRefreshTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return;

        await userManager.RemoveAuthenticationTokenAsync(user, RefreshLoginProvider, RefreshTokenName);
        await userManager.RemoveAuthenticationTokenAsync(user, RefreshLoginProvider, RefreshTokenExpiryName);
    }

    public async Task<ResetPasswordGatewayResult> ResetPasswordAsync(string email, string encodedToken, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return new ResetPasswordGatewayResult(false, ["Invalid password reset request."]);

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
        catch
        {
            return new ResetPasswordGatewayResult(false, ["Invalid reset token format."]);
        }

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);

        return result.Succeeded
            ? new ResetPasswordGatewayResult(true, Array.Empty<string>())
            : new ResetPasswordGatewayResult(false, result.Errors.Select(x => x.Description).ToArray());
    }

    public async Task<ChangePasswordGatewayResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return new ChangePasswordGatewayResult(false, ["User not found."]);

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        return result.Succeeded
            ? new ChangePasswordGatewayResult(true, Array.Empty<string>())
            : new ChangePasswordGatewayResult(false, result.Errors.Select(x => x.Description).ToArray());
    }

    public async Task<ChangeEmailGatewayResult> ChangeEmailAsync(string userId, string newEmail, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return new ChangeEmailGatewayResult(false, ["User not found."]);

        var passwordIsValid = await userManager.CheckPasswordAsync(user, password);
        if (!passwordIsValid)
            return new ChangeEmailGatewayResult(false, ["Invalid password."]);

        var existing = await userManager.FindByEmailAsync(newEmail);
        if (existing is not null && !string.Equals(existing.Id, userId, StringComparison.Ordinal))
            return new ChangeEmailGatewayResult(false, ["User with this email already exists."]);

        var setEmailResult = await userManager.SetEmailAsync(user, newEmail);
        if (!setEmailResult.Succeeded)
            return new ChangeEmailGatewayResult(false, setEmailResult.Errors.Select(x => x.Description).ToArray());

        var setUserNameResult = await userManager.SetUserNameAsync(user, newEmail);
        if (!setUserNameResult.Succeeded)
            return new ChangeEmailGatewayResult(false, setUserNameResult.Errors.Select(x => x.Description).ToArray());

        // Changing email with current password is a trusted action, keep email confirmed.
        user.EmailConfirmed = true;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return new ChangeEmailGatewayResult(false, updateResult.Errors.Select(x => x.Description).ToArray());

        return new ChangeEmailGatewayResult(true, Array.Empty<string>());
    }

    public async Task<ExternalLoginGatewayResult> LoginOrCreateExternalAsync(
        string provider,
        string providerUserId,
        string? email,
        string role,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var normalizedProviderUserId = providerUserId.Trim();
        var normalizedRole = string.Equals(role, "Driver", StringComparison.OrdinalIgnoreCase)
            ? "Driver"
            : "Passenger";

        if (string.IsNullOrWhiteSpace(normalizedProvider) || string.IsNullOrWhiteSpace(normalizedProviderUserId))
            return new ExternalLoginGatewayResult(false, null, null, null, ["External provider data is invalid."]);

        ApplicationUser? user = await userManager.FindByLoginAsync(normalizedProvider, normalizedProviderUserId);

        var normalizedEmail = string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToLowerInvariant();

        if (user is null && !string.IsNullOrWhiteSpace(normalizedEmail))
            user = await userManager.FindByEmailAsync(normalizedEmail);

        if (user is null)
        {
            var fallbackEmail = BuildFallbackEmail(normalizedProvider, normalizedProviderUserId);

            user = new ApplicationUser
            {
                Email = normalizedEmail ?? fallbackEmail,
                UserName = normalizedEmail ?? fallbackEmail,
                EmailConfirmed = true,
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return new ExternalLoginGatewayResult(
                    false,
                    null,
                    null,
                    null,
                    createResult.Errors.Select(x => x.Description).ToArray());
        }

        var existingLogins = await userManager.GetLoginsAsync(user);
        var hasCurrentLogin = existingLogins.Any(login =>
            string.Equals(login.LoginProvider, normalizedProvider, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(login.ProviderKey, normalizedProviderUserId, StringComparison.Ordinal));

        if (!hasCurrentLogin)
        {
            var addLoginResult = await userManager.AddLoginAsync(
                user,
                new UserLoginInfo(normalizedProvider, normalizedProviderUserId, normalizedProvider));

            if (!addLoginResult.Succeeded)
                return new ExternalLoginGatewayResult(
                    false,
                    null,
                    null,
                    null,
                    addLoginResult.Errors.Select(x => x.Description).ToArray());
        }

        if (!await roleManager.RoleExistsAsync(normalizedRole))
        {
            var roleCreateResult = await roleManager.CreateAsync(new IdentityRole(normalizedRole));
            if (!roleCreateResult.Succeeded)
                return new ExternalLoginGatewayResult(
                    false,
                    null,
                    null,
                    null,
                    roleCreateResult.Errors.Select(x => x.Description).ToArray());
        }

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count == 0)
        {
            var addRoleResult = await userManager.AddToRoleAsync(user, normalizedRole);
            if (!addRoleResult.Succeeded)
                return new ExternalLoginGatewayResult(
                    false,
                    null,
                    null,
                    null,
                    addRoleResult.Errors.Select(x => x.Description).ToArray());

            roles = await userManager.GetRolesAsync(user);
        }

        var resolvedRole = roles.FirstOrDefault() ?? normalizedRole;

        var resolvedEmail = user.Email;
        if (string.IsNullOrWhiteSpace(resolvedEmail))
        {
            resolvedEmail = BuildFallbackEmail(normalizedProvider, normalizedProviderUserId);
            user.Email = resolvedEmail;
            user.UserName = resolvedEmail;
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        return new ExternalLoginGatewayResult(
            true,
            user.Id,
            resolvedEmail,
            resolvedRole,
            Array.Empty<string>());
    }

    private static string BuildFallbackEmail(string provider, string providerUserId)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{provider}:{providerUserId}"));
        var suffix = Convert.ToHexString(hashBytes).ToLowerInvariant()[..20];
        return $"{provider}_{suffix}@oauth.local";
    }
}
