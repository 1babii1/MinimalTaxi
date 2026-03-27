namespace AuthService.Application.Abstractions;

public interface IJwtTokenProvider
{
    string CreateAccessToken(string userId, string email, string role);
    string CreateRefreshToken(string userId);
}
