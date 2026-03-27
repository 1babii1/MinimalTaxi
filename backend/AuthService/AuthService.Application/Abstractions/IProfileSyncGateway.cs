namespace AuthService.Application.Abstractions;

public interface IProfileSyncGateway
{
    Task SyncRegistrationProfileAsync(
        string userId,
        RegistrationProfileData profileData,
        CancellationToken cancellationToken = default);
}
