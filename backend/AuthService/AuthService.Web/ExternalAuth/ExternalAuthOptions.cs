namespace AuthService.Web.ExternalAuth;

public sealed class ExternalAuthOptions
{
    public const string SectionName = "ExternalAuth";

    public ExternalProviderOptions Yandex { get; set; } = new();
    public ExternalProviderOptions Vk { get; set; } = new();

    public sealed class ExternalProviderOptions
    {
        public bool Enabled { get; set; } = false;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string AuthorizationEndpoint { get; set; } = string.Empty;
        public string TokenEndpoint { get; set; } = string.Empty;
        public string UserInfoEndpoint { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "5.199";
    }
}
