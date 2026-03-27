using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinimalTaxiService.Infrastructure.Postgres.Migrations
{
    public partial class AddDepartureAtToIntercityTripDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "departure_at",
                table: "intercity_trip_details",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_intercity_trip_details_departure_at",
                table: "intercity_trip_details",
                column: "departure_at");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_intercity_trip_details_departure_at",
                table: "intercity_trip_details");

            migrationBuilder.DropColumn(
                name: "departure_at",
                table: "intercity_trip_details");
        }
    }
}
