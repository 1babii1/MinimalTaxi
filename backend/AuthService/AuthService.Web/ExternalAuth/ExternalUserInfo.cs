namespace AuthService.Web.ExternalAuth;

public sealed record ExternalUserInfo(string ProviderUserId, string? Email);
