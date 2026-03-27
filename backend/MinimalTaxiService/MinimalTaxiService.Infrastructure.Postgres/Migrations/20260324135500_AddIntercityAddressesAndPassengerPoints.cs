using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace MinimalTaxiService.Infrastructure.Postgres.Migrations
{
    public partial class AddIntercityAddressesAndPassengerPoints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "from_address",
                table: "intercity_trip_details",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "to_address",
                table: "intercity_trip_details",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_address",
                table: "trip_participants",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dropoff_address",
                table: "trip_participants",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "pickup_location",
                table: "trip_participants",
                type: "geography (point, 4326)",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "dropoff_location",
                table: "trip_participants",
                type: "geography (point, 4326)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "from_address", table: "intercity_trip_details");
            migrationBuilder.DropColumn(name: "to_address", table: "intercity_trip_details");
            migrationBuilder.DropColumn(name: "pickup_address", table: "trip_participants");
            migrationBuilder.DropColumn(name: "dropoff_address", table: "trip_participants");
            migrationBuilder.DropColumn(name: "pickup_location", table: "trip_participants");
            migrationBuilder.DropColumn(name: "dropoff_location", table: "trip_participants");
        }
    }
}
