using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinimalTaxiService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalFromToAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "from_address",
                table: "trips",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "to_address",
                table: "trips",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "from_address",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "to_address",
                table: "trips");
        }
    }
}
