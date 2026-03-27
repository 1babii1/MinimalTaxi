using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace MinimalTaxiService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "driver_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location = table.Column<Point>(type: "geography (point, 4326)", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_driver_locations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "intercity_trip_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_location = table.Column<Point>(type: "geography (point, 4326)", nullable: false),
                    to_location = table.Column<Point>(type: "geography (point, 4326)", nullable: false),
                    total_seats = table.Column<int>(type: "integer", nullable: false),
                    available_seats = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_intercity_trip_details", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    passenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    origin_location = table.Column<Point>(type: "geography (point, 4326)", nullable: false),
                    destination_location = table.Column<Point>(type: "geography (point, 4326)", nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trips", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    street = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    house = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    apartment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    car_brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    car_model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    car_color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    car_plate_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trip_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_driver = table.Column<bool>(type: "boolean", nullable: false),
                    booked_seats = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trip_participants", x => x.id);
                    table.ForeignKey(
                        name: "FK_trip_participants_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_driver_locations_location_gist",
                table: "driver_locations",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_driver_locations_timestamp",
                table: "driver_locations",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ux_driver_locations_driver_id",
                table: "driver_locations",
                column: "driver_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_intercity_trip_details_available_seats",
                table: "intercity_trip_details",
                column: "available_seats");

            migrationBuilder.CreateIndex(
                name: "ix_intercity_trip_details_from_location_gist",
                table: "intercity_trip_details",
                column: "from_location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_intercity_trip_details_to_location_gist",
                table: "intercity_trip_details",
                column: "to_location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ux_intercity_trip_details_trip_id",
                table: "intercity_trip_details",
                column: "trip_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trip_participants_trip_id",
                table: "trip_participants",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_trip_participants_trip_id_is_driver",
                table: "trip_participants",
                columns: new[] { "trip_id", "is_driver" });

            migrationBuilder.CreateIndex(
                name: "ux_trip_participants_trip_id_user_id",
                table: "trip_participants",
                columns: new[] { "trip_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trips_city_type_status",
                table: "trips",
                columns: new[] { "city", "type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_trips_created_at",
                table: "trips",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_trips_destination_location_gist",
                table: "trips",
                column: "destination_location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_trips_driver_id_status",
                table: "trips",
                columns: new[] { "driver_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_trips_origin_location_gist",
                table: "trips",
                column: "origin_location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_trips_passenger_id_status",
                table: "trips",
                columns: new[] { "passenger_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_users_created_at",
                table: "users",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_users_phone_number",
                table: "users",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                table: "users",
                column: "role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "driver_locations");

            migrationBuilder.DropTable(
                name: "intercity_trip_details");

            migrationBuilder.DropTable(
                name: "trip_participants");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "trips");
        }
    }
}
