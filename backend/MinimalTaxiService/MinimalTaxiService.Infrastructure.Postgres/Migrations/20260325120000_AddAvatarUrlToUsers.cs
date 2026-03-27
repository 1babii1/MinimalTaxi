using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinimalTaxiService.Infrastructure.Postgres.Migrations
{
    public partial class AddAvatarUrlToUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE users ADD COLUMN IF NOT EXISTS avatar_url character varying(500);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avatar_url",
                table: "users");
        }
    }
}
