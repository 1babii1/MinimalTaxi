using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace MinimalTaxiService.Web.Integrations.Storage;

public sealed class SelectelS3StorageService
{
    private static readonly HashSet<string> AllowedImageTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    ];

    private readonly IAmazonS3 _s3Client;
    private readonly SelectelS3Options _options;

    public SelectelS3StorageService(IAmazonS3 s3Client, IOptions<SelectelS3Options> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public async Task<string> UploadAvatarAsync(IFormFile file, Guid userId, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("Empty file");

        if (!AllowedImageTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Only image files are allowed");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Avatar must be <= 5MB");

        var bucketName = _options.BucketName?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("S3 BucketName is not configured");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg";

        var key = $"avatars/{userId}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        await using var stream = file.OpenReadStream();
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = stream,
            ContentType = file.ContentType,
            CannedACL = S3CannedACL.PublicRead,
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        var endpoint = _options.Endpoint?.TrimEnd('/');
        var directUrl = string.IsNullOrWhiteSpace(endpoint)
            ? $"/{bucketName}/{key}"
            : $"{endpoint}/{bucketName}/{key}";

        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            var baseUrl = _options.PublicBaseUrl.TrimEnd('/');

            if (baseUrl.Contains("{bucket}", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = baseUrl.Replace("{bucket}", bucketName, StringComparison.OrdinalIgnoreCase);
                return $"{baseUrl}/{key}";
            }

            return $"{baseUrl}/{key}";
        }

        return directUrl;
    }
}
