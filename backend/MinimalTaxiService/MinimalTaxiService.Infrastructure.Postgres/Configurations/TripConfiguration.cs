using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalTaxiService.Domain.Entities;
using MinimalTaxiService.Domain.Enums;
using Shared;

namespace MinimalTaxiService.Infrastructure.Postgres.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips");

        builder.HasKey(trip => trip.Id).HasName("pk_trips");

        builder.Property(trip => trip.Id)
            .HasColumnName("id");

        builder.Property(trip => trip.PassengerId)
            .HasColumnName("passenger_id");

        builder.Property(trip => trip.DriverId)
            .HasColumnName("driver_id")
            .IsRequired(false);

        builder.Property(trip => trip.Type)
            .HasConversion(type => type.ToString(), value => Enum.Parse<TripType>(value))
            .HasMaxLength(LenghtConstants.LENGTH20)
            .IsRequired()
            .HasColumnName("type");

        builder.Property(trip => trip.Status)
            .HasConversion(status => status.ToString(), value => Enum.Parse<TripStatus>(value))
            .HasMaxLength(LenghtConstants.LENGTH30)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(trip => trip.Origin)
            .HasConversion(LocationPointConverter.Required)
            .HasColumnType("geography (point, 4326)")
            .HasColumnName("origin_location");

        builder.Property(trip => trip.Destination)
            .HasConversion(LocationPointConverter.Nullable)
            .HasColumnType("geography (point, 4326)")
            .HasColumnName("destination_location")
            .IsRequired(false);

        builder.Property(trip => trip.FromAddress)
            .HasMaxLength(LenghtConstants.LENGTH150)
            .HasColumnName("from_address")
            .IsRequired(false);

        builder.Property(trip => trip.ToAddress)
            .HasMaxLength(LenghtConstants.LENGTH150)
            .HasColumnName("to_address")
            .IsRequired(false);

        builder.Property(trip => trip.City)
            .HasMaxLength(LenghtConstants.LENGTH100)
            .HasColumnName("city")
            .IsRequired(false);

        builder.Property(trip => trip.Description)
            .HasMaxLength(LenghtConstants.LENGTH500)
            .HasColumnName("description")
            .IsRequired(false);

        builder.Property(trip => trip.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(trip => trip.UpdatedAt)
            .IsRequired(false)
            .HasColumnName("updated_at");

        builder.HasMany(trip => trip.Participants)
            .WithOne()
            .HasForeignKey(participant => participant.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(trip => new { trip.PassengerId, trip.Status })
            .HasDatabaseName("ix_trips_passenger_id_status");

        builder.HasIndex(trip => new { trip.DriverId, trip.Status })
            .HasDatabaseName("ix_trips_driver_id_status");

        builder.HasIndex(trip => new { trip.City, trip.Type, trip.Status })
            .HasDatabaseName("ix_trips_city_type_status");

        builder.HasIndex(trip => trip.CreatedAt)
            .HasDatabaseName("ix_trips_created_at");

        builder.HasIndex(trip => trip.Origin)
            .HasMethod("gist")
            .HasDatabaseName("ix_trips_origin_location_gist");

        builder.HasIndex(trip => trip.Destination)
            .HasMethod("gist")
            .HasDatabaseName("ix_trips_destination_location_gist");
    }
}