using AuthService.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.PostgreSql.Urls;

public class FrontendUrlProvider(IOptions<FrontendOptions> options) : IFrontendUrlProvider
{
    public string BuildConfirmEmailUrl(string userId, string encodedToken)
    {
        var baseUrl = options.Value.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/auth/confirm-email?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(encodedToken)}";
    }

    public string BuildResetPasswordUrl(string email, string encodedToken)
    {
        var baseUrl = options.Value.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/auth/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(encodedToken)}";
    }
}
