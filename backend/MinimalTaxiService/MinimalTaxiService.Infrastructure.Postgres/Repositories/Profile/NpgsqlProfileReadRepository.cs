using Dapper;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Contracts.Profiles;

namespace MinimalTaxiService.Infrastructure.Postgres.Repositories.Profile;

public class NpgsqlProfileReadRepository : IProfileReadRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public NpgsqlProfileReadRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ProfileDto?> GetProfile(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT
                               id AS UserId,
                               display_name AS Name,
                               avatar_url AS AvatarUrl,
                               phone_number AS Phone,
                               role AS Role,
                               city,
                               street,
                               house,
                               apartment,
                               car_brand AS Brand,
                               car_model AS Model,
                               car_color AS Color,
                               car_plate_number AS PlateNumber
                           FROM users
                           WHERE id = @UserId
                           LIMIT 1
                           """;

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var raw = await connection.QueryFirstOrDefaultAsync<ProfileRaw>(sql, new { UserId = userId });
        if (raw is null)
            return null;

        return new ProfileDto
        {
            UserId = raw.UserId,
            Name = raw.Name,
            AvatarUrl = raw.AvatarUrl,
            Phone = raw.Phone,
            Role = raw.Role,
            Address = string.IsNullOrWhiteSpace(raw.City) && string.IsNullOrWhiteSpace(raw.Street)
                ? null
                : new AddressDto
                {
                    City = raw.City ?? string.Empty,
                    Street = raw.Street ?? string.Empty,
                    House = raw.House ?? string.Empty,
                    Apartment = raw.Apartment
                },
            CarInfo = string.IsNullOrWhiteSpace(raw.Brand)
                ? null
                : new CarInfoDto
                {
                    Brand = raw.Brand ?? string.Empty,
                    Model = raw.Model ?? string.Empty,
                    Color = raw.Color ?? string.Empty,
                    PlateNumber = raw.PlateNumber ?? string.Empty
                }
        };
    }

    private sealed class ProfileRaw
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? House { get; set; }
        public string? Apartment { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Color { get; set; }
        public string? PlateNumber { get; set; }
    }
}
