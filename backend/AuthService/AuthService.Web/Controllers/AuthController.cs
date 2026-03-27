using AuthService.Application.Abstractions;
using AuthService.Infrastructure.PostgreSql.Urls;
using AuthService.Web.ExternalAuth;
using AuthService.Infrastructure.PostgreSql.Identity;
using AuthService.Contracts.Requests;
using AuthService.Contracts.Responses;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace AuthService.Web.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IOptions<JwtOptions> jwtOptions,
    IOptions<FrontendOptions> frontendOptions,
    IDataProtectionProvider dataProtectionProvider,
    ExternalOAuthClient externalOAuthClient) : ControllerBase
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly FrontendOptions _frontendOptions = frontendOptions.Value;
    private readonly IDataProtector _stateProtector = dataProtectionProvider.CreateProtector("auth.external.state.v1");

    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        Response.Cookies.Append(_jwtOptions.AccessTokenCookieName, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes)
        });

        Response.Cookies.Append(_jwtOptions.RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays)
        });
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete(_jwtOptions.AccessTokenCookieName);
        Response.Cookies.Delete(_jwtOptions.RefreshTokenCookieName);
    }

    // Creates a new account and sends an email confirmation link.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var profileData = new RegistrationProfileData(
            request.Name,
            request.Phone,
            request.Role,
            request.Address is null
                ? null
                : new RegistrationAddressData(
                    request.Address.City,
                    request.Address.Street,
                    request.Address.House,
                    request.Address.Apartment),
            request.CarInfo is null
                ? null
                : new RegistrationCarInfoData(
                    request.CarInfo.Brand,
                    request.CarInfo.Model,
                    request.CarInfo.Color,
                    request.CarInfo.PlateNumber));

        var result = await authService.RegisterAsync(
            request.Email,
            request.Password,
            request.Role,
            profileData,
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ApiResponse(false, result.Message, result.Errors));

        return Ok(new ApiResponse(true, result.Message));
    }

    [HttpGet("external/{provider}/start")]
    public IActionResult StartExternalLogin([FromRoute] string provider, [FromQuery] string? role = null)
    {
        var normalizedRole = string.Equals(role, "Driver", StringComparison.OrdinalIgnoreCase)
            ? "Driver"
            : "Passenger";

        var state = CreateState(provider, normalizedRole);
        if (!externalOAuthClient.TryBuildAuthorizeUrl(provider, state, out var authorizeUrl, out var error))
            return BadRequest(new ApiResponse(false, error ?? "Failed to build external authorization URL."));

        return Redirect(authorizeUrl!);
    }

    [HttpGet("external/{provider}/callback")]
    public async Task<IActionResult> ExternalCallback(
        [FromRoute] string provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
            return Redirect(BuildFrontendErrorRedirect($"{provider}: {error}"));

        if (!TryReadState(state, provider, out var role))
            return Redirect(BuildFrontendErrorRedirect("Invalid external auth state."));

        var exchangeResult = await externalOAuthClient.ExchangeCodeAsync(provider, code ?? string.Empty, cancellationToken);
        if (exchangeResult.user is null)
            return Redirect(BuildFrontendErrorRedirect(exchangeResult.error ?? "External auth failed."));

        var loginResult = await authService.LoginExternalAsync(
            provider,
            exchangeResult.user.ProviderUserId,
            exchangeResult.user.Email,
            role ?? "Passenger",
            cancellationToken);

        if (!loginResult.IsSuccess || string.IsNullOrWhiteSpace(loginResult.AccessToken) || string.IsNullOrWhiteSpace(loginResult.RefreshToken))
            return Redirect(BuildFrontendErrorRedirect(loginResult.Message));

        SetAuthCookies(loginResult.AccessToken, loginResult.RefreshToken);
        return Redirect(BuildFrontendSuccessRedirect());
    }

    // Confirms user email by token from email link.
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ConfirmEmailAsync(request.UserId, request.Token, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ApiResponse(false, result.Message, result.Errors));

        return Ok(new ApiResponse(true, result.Message));
    }

    // Validates credentials and checks email confirmation.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request.Email, request.Password, cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(new ApiResponse(false, result.Message, result.Errors));

        SetAuthCookies(result.AccessToken!, result.RefreshToken!);
        return Ok(new ApiResponse(true, result.Message));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(_jwtOptions.RefreshTokenCookieName, out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(new ApiResponse(false, "Refresh token is required."));

        var result = await authService.RefreshAsync(refreshToken, cancellationToken);
        if (!result.IsSuccess)
        {
            ClearAuthCookies();
            return Unauthorized(new ApiResponse(false, result.Message, result.Errors));
        }

        SetAuthCookies(result.AccessToken!, result.RefreshToken!);
        return Ok(new ApiResponse(true, result.Message));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(_jwtOptions.RefreshTokenCookieName, out var refreshToken);
        await authService.LogoutAsync(refreshToken, cancellationToken);
        ClearAuthCookies();
        return Ok(new ApiResponse(true, "Logged out."));
    }

    [Authorize]
    [HttpGet("session")]
    public IActionResult Session()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new SessionResponse(true, userId, email, role));
    }

    // Sends reset-password link if account exists and email is confirmed.
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ForgotPasswordAsync(request.Email, cancellationToken);
        return Ok(new ApiResponse(true, result.Message));
    }

    // Resets password using token from email link.
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ApiResponse(false, result.Message, result.Errors));

        return Ok(new ApiResponse(true, result.Message));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await authService.ChangePasswordAsync(
            userId ?? string.Empty,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ApiResponse(false, result.Message, result.Errors));

        return Ok(new ApiResponse(true, result.Message));
    }

    [Authorize]
    [HttpPost("change-email")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await authService.ChangeEmailAsync(
            userId ?? string.Empty,
            request.NewEmail,
            request.Password,
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new ApiResponse(false, result.Message, result.Errors));

        ClearAuthCookies();
        return Ok(new ApiResponse(true, result.Message));
    }

    private string BuildFrontendSuccessRedirect()
    {
        var baseUrl = _frontendOptions.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/";
    }

    private string BuildFrontendErrorRedirect(string message)
    {
        var baseUrl = _frontendOptions.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/auth/login";
        return QueryHelpers.AddQueryString(url, "socialError", message);
    }

    private string CreateState(string provider, string role)
    {
        var payload = new ExternalAuthState(provider.ToLowerInvariant(), role, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        var protectedBytes = _stateProtector.Protect(bytes);
        return WebEncoders.Base64UrlEncode(protectedBytes);
    }

    private bool TryReadState(string? state, string provider, out string? role)
    {
        role = null;

        if (string.IsNullOrWhiteSpace(state))
            return false;

        try
        {
            var raw = WebEncoders.Base64UrlDecode(state);
            var unprotected = _stateProtector.Unprotect(raw);
            var json = Encoding.UTF8.GetString(unprotected);
            var payload = JsonSerializer.Deserialize<ExternalAuthState>(json);

            if (payload is null)
                return false;

            if (!string.Equals(payload.Provider, provider, StringComparison.OrdinalIgnoreCase))
                return false;

            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(payload.IssuedAtUnix);
            if (DateTimeOffset.UtcNow - issuedAt > TimeSpan.FromMinutes(10))
                return false;

            role = string.Equals(payload.Role, "Driver", StringComparison.OrdinalIgnoreCase)
                ? "Driver"
                : "Passenger";

            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed record ExternalAuthState(string Provider, string Role, long IssuedAtUnix);
}
