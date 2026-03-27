namespace AuthService.Application.Abstractions;

public interface IFrontendUrlProvider
{
    string BuildConfirmEmailUrl(string userId, string encodedToken);
    string BuildResetPasswordUrl(string email, string encodedToken);
}
