namespace AuthService.Infrastructure.PostgreSql.Integrations.ProfileSync;

public sealed class ProfileSyncOptions
{
    public const string SectionName = "ProfileSync";

    public string BaseUrl { get; set; } = "http://localhost:5290";
    public string InternalKey { get; set; } = string.Empty;
}
