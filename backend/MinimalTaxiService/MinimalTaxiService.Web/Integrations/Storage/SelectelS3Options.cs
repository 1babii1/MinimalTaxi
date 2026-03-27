namespace MinimalTaxiService.Web.Integrations.Storage;

public sealed class SelectelS3Options
{
    public string Region { get; set; } = "ru-3";
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
}
