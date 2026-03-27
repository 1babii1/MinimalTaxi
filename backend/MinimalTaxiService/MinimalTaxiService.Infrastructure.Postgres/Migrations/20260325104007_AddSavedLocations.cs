using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinimalTaxiService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE users ADD COLUMN IF NOT EXISTS avatar_url character varying(500);");
            migrationBuilder.Sql("ALTER TABLE trip_participants ADD COLUMN IF NOT EXISTS dropoff_address character varying(150);");
            migrationBuilder.Sql("ALTER TABLE trip_participants ADD COLUMN IF NOT EXISTS dropoff_location geography(point, 4326);");
            migrationBuilder.Sql("ALTER TABLE trip_participants ADD COLUMN IF NOT EXISTS pickup_address character varying(150);");
            migrationBuilder.Sql("ALTER TABLE trip_participants ADD COLUMN IF NOT EXISTS pickup_location geography(point, 4326);");
            migrationBuilder.Sql("ALTER TABLE intercity_trip_details ADD COLUMN IF NOT EXISTS departure_at timestamp with time zone;");
            migrationBuilder.Sql("ALTER TABLE intercity_trip_details ADD COLUMN IF NOT EXISTS from_address character varying(150);");
            migrationBuilder.Sql("ALTER TABLE intercity_trip_details ADD COLUMN IF NOT EXISTS required_seats integer;");
            migrationBuilder.Sql("ALTER TABLE intercity_trip_details ADD COLUMN IF NOT EXISTS to_address character varying(150);");

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS saved_locations (
                    id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    name character varying(120) NOT NULL,
                    address character varying(500) NOT NULL,
                    location geography(point, 4326) NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_saved_locations PRIMARY KEY (id)
                );
                """);

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_intercity_trip_details_departure_at ON intercity_trip_details (departure_at);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_saved_locations_created_at ON saved_locations (created_at);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_saved_locations_user_id ON saved_locations (user_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_saved_locations_location_gist ON saved_locations USING gist (location);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saved_locations");
        }
    }
}
