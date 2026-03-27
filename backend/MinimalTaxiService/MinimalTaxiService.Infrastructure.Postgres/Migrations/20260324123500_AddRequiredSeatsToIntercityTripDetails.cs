using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinimalTaxiService.Infrastructure.Postgres.Migrations
{
    public partial class AddRequiredSeatsToIntercityTripDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "required_seats",
                table: "intercity_trip_details",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "required_seats",
                table: "intercity_trip_details");
        }
    }
}
