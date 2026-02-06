using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthServer.Migrations
{
    public partial class AddClientPresetProfileStateFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppliedPresetId",
                table: "ClientStates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AppliedPresetVersion",
                table: "ClientStates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverridesJson",
                table: "ClientStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Profile",
                table: "ClientStates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SystemManaged",
                table: "ClientStates",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AppliedPresetId", table: "ClientStates");
            migrationBuilder.DropColumn(name: "AppliedPresetVersion", table: "ClientStates");
            migrationBuilder.DropColumn(name: "OverridesJson", table: "ClientStates");
            migrationBuilder.DropColumn(name: "Profile", table: "ClientStates");
            migrationBuilder.DropColumn(name: "SystemManaged", table: "ClientStates");
        }
    }
}
