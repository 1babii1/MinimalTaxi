namespace AuthService.Infrastructure.PostgreSql.Identity;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "MinimalTaxi.Auth";
    public string Audience { get; set; } = "MinimalTaxi.Client";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 30;
    public string AccessTokenCookieName { get; set; } = "mt_access_token";
    public string RefreshTokenCookieName { get; set; } = "mt_refresh_token";
}
