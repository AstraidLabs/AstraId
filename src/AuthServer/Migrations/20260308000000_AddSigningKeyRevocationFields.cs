using System;
using AuthServer.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthServer.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260308000000_AddSigningKeyRevocationFields")]
public partial class AddSigningKeyRevocationFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "RetireAfterUtc",
            table: "SigningKeyRingEntries",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "RevokedUtc",
            table: "SigningKeyRingEntries",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RetireAfterUtc",
            table: "SigningKeyRingEntries");

        migrationBuilder.DropColumn(
            name: "RevokedUtc",
            table: "SigningKeyRingEntries");
    }
}
