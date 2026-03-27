using System.Net.Http.Json;
using AuthService.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.PostgreSql.Integrations.ProfileSync;

public sealed class TaxiProfileSyncGateway(
    HttpClient httpClient,
    IOptions<ProfileSyncOptions> options) : IProfileSyncGateway
{
    private readonly ProfileSyncOptions _options = options.Value;

    public async Task SyncRegistrationProfileAsync(
        string userId,
        RegistrationProfileData profileData,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
            return;

        var request = new BootstrapProfileRequest(
            parsedUserId,
            profileData.Role,
            profileData.Name ?? string.Empty,
            profileData.Phone,
            profileData.Address is null
                ? null
                : new BootstrapAddressRequest(
                    profileData.Address.City,
                    profileData.Address.Street,
                    profileData.Address.House,
                    profileData.Address.Apartment),
            profileData.CarInfo is null
                ? null
                : new BootstrapCarInfoRequest(
                    profileData.CarInfo.Brand,
                    profileData.CarInfo.Model,
                    profileData.CarInfo.Color,
                    profileData.CarInfo.PlateNumber));

        using var message = new HttpRequestMessage(HttpMethod.Post, "internal/profile/bootstrap")
        {
            Content = JsonContent.Create(request)
        };

        if (!string.IsNullOrWhiteSpace(_options.InternalKey))
            message.Headers.TryAddWithoutValidation("X-Internal-Key", _options.InternalKey);

        using var response = await httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed record BootstrapProfileRequest(
        Guid UserId,
        string Role,
        string Name,
        string? Phone,
        BootstrapAddressRequest? Address,
        BootstrapCarInfoRequest? CarInfo);

    private sealed record BootstrapAddressRequest(
        string? City,
        string? Street,
        string? House,
        string? Apartment);

    private sealed record BootstrapCarInfoRequest(
        string? Brand,
        string? Model,
        string? Color,
        string? PlateNumber);
}
