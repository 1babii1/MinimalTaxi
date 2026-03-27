using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalTaxiService.Domain.Entities;
using Shared;

namespace MinimalTaxiService.Infrastructure.Postgres.Configurations;

public class SavedLocationConfiguration : IEntityTypeConfiguration<SavedLocation>
{
    public void Configure(EntityTypeBuilder<SavedLocation> builder)
    {
        builder.ToTable("saved_locations");

        builder.HasKey(savedLocation => savedLocation.Id)
            .HasName("pk_saved_locations");

        builder.Property(savedLocation => savedLocation.Id)
            .HasColumnName("id");

        builder.Property(savedLocation => savedLocation.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(savedLocation => savedLocation.Name)
            .IsRequired()
            .HasMaxLength(LenghtConstants.LENGTH120)
            .HasColumnName("name");

        builder.Property(savedLocation => savedLocation.Address)
            .IsRequired()
            .HasMaxLength(LenghtConstants.LENGTH500)
            .HasColumnName("address");

        builder.Property(savedLocation => savedLocation.Location)
            .HasConversion(LocationPointConverter.Required)
            .HasColumnType("geography (point, 4326)")
            .HasColumnName("location");

        builder.Property(savedLocation => savedLocation.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.HasIndex(savedLocation => savedLocation.UserId)
            .HasDatabaseName("ix_saved_locations_user_id");

        builder.HasIndex(savedLocation => savedLocation.Location)
            .HasMethod("gist")
            .HasDatabaseName("ix_saved_locations_location_gist");

        builder.HasIndex(savedLocation => savedLocation.CreatedAt)
            .HasDatabaseName("ix_saved_locations_created_at");
    }
}