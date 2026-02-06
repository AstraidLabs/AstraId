using AuthServer.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthServer.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260312000000_AddJwksCacheMarginMinutes")]
public partial class AddJwksCacheMarginMinutes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "JwksCacheMarginMinutes",
            table: "KeyRotationPolicies",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "JwksCacheMarginMinutes",
            table: "KeyRotationPolicies");
    }
}
