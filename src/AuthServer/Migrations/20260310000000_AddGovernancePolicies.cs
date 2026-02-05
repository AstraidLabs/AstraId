using System;
using AuthServer.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthServer.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260310000000_AddGovernancePolicies")]
public partial class AddGovernancePolicies : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Comment",
            table: "SigningKeyRingEntries",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "CreatedByUserId",
            table: "SigningKeyRingEntries",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Thumbprint",
            table: "SigningKeyRingEntries",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "KeyRotationPolicies",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Enabled = table.Column<bool>(type: "boolean", nullable: false),
                RotationIntervalDays = table.Column<int>(type: "integer", nullable: false),
                GracePeriodDays = table.Column<int>(type: "integer", nullable: false),
                NextRotationUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastRotationUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KeyRotationPolicies", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TokenIncidents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                ClientId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                TraceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                DetailJson = table.Column<string>(type: "text", nullable: true),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TokenIncidents", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TokenPolicies",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccessTokenMinutes = table.Column<int>(type: "integer", nullable: false),
                IdentityTokenMinutes = table.Column<int>(type: "integer", nullable: false),
                AuthorizationCodeMinutes = table.Column<int>(type: "integer", nullable: false),
                RefreshTokenDays = table.Column<int>(type: "integer", nullable: false),
                RefreshRotationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                RefreshReuseDetectionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                RefreshReuseLeewaySeconds = table.Column<int>(type: "integer", nullable: false),
                ClockSkewSeconds = table.Column<int>(type: "integer", nullable: false),
                UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TokenPolicies", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_KeyRotationPolicies_UpdatedUtc",
            table: "KeyRotationPolicies",
            column: "UpdatedUtc");

        migrationBuilder.CreateIndex(
            name: "IX_TokenIncidents_Severity",
            table: "TokenIncidents",
            column: "Severity");

        migrationBuilder.CreateIndex(
            name: "IX_TokenIncidents_TimestampUtc",
            table: "TokenIncidents",
            column: "TimestampUtc");

        migrationBuilder.CreateIndex(
            name: "IX_TokenIncidents_Type",
            table: "TokenIncidents",
            column: "Type");

        migrationBuilder.CreateIndex(
            name: "IX_TokenPolicies_UpdatedUtc",
            table: "TokenPolicies",
            column: "UpdatedUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "KeyRotationPolicies");

        migrationBuilder.DropTable(
            name: "TokenIncidents");

        migrationBuilder.DropTable(
            name: "TokenPolicies");

        migrationBuilder.DropColumn(
            name: "Comment",
            table: "SigningKeyRingEntries");

        migrationBuilder.DropColumn(
            name: "CreatedByUserId",
            table: "SigningKeyRingEntries");

        migrationBuilder.DropColumn(
            name: "Thumbprint",
            table: "SigningKeyRingEntries");
    }
}
