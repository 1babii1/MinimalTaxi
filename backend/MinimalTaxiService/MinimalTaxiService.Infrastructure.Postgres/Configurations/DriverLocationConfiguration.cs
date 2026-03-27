using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalTaxiService.Domain.Entities;

namespace MinimalTaxiService.Infrastructure.Postgres.Configurations;

public class DriverLocationConfiguration : IEntityTypeConfiguration<DriverLocation>
{
    public void Configure(EntityTypeBuilder<DriverLocation> builder)
    {
        builder.ToTable("driver_locations");

        builder.HasKey(driverLocation => driverLocation.Id).HasName("pk_driver_locations");

        builder.Property(driverLocation => driverLocation.Id)
            .HasColumnName("id");

        builder.Property(driverLocation => driverLocation.DriverId)
            .IsRequired()
            .HasColumnName("driver_id");

        builder.Property(driverLocation => driverLocation.Location)
            .HasConversion(LocationPointConverter.Required)
            .HasColumnType("geography (point, 4326)")
            .HasColumnName("location");

        builder.Property(driverLocation => driverLocation.Timestamp)
            .IsRequired()
            .HasColumnName("timestamp");

        builder.HasIndex(driverLocation => driverLocation.DriverId)
            .IsUnique()
            .HasDatabaseName("ux_driver_locations_driver_id");

        builder.HasIndex(driverLocation => driverLocation.Timestamp)
            .HasDatabaseName("ix_driver_locations_timestamp");

        builder.HasIndex(driverLocation => driverLocation.Location)
            .HasMethod("gist")
            .HasDatabaseName("ix_driver_locations_location_gist");
    }
}