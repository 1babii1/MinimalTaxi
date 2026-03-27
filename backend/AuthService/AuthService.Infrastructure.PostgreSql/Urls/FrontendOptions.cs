namespace AuthService.Infrastructure.PostgreSql.Urls;

public class FrontendOptions
{
    public const string SectionName = "Frontend";

    public string BaseUrl { get; set; } = "http://localhost:5173";
}
