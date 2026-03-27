namespace AuthService.Infrastructure.PostgreSql.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; }
    public string From { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Pass { get; set; } = string.Empty;
}
