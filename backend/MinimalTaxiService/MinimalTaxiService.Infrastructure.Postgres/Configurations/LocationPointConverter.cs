using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using DomainLocation = MinimalTaxiService.Domain.ValueObjects.Location;

namespace MinimalTaxiService.Infrastructure.Postgres.Configurations;

internal static class LocationPointConverter
{
    public static readonly ValueConverter<DomainLocation, Point> Required = new(
        location => new Point(location.Longitude, location.Latitude) { SRID = 4326 },
        point => DomainLocation.Create(point.Y, point.X).Value);

    public static readonly ValueConverter<DomainLocation?, Point?> Nullable = new(
        location => location == null
            ? null
            : new Point(location.Longitude, location.Latitude) { SRID = 4326 },
        point => point == null
            ? null
            : DomainLocation.Create(point.Y, point.X).Value);
}
