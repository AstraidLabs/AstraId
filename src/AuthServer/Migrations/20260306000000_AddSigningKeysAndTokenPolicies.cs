using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthServer.Migrations
{
    public partial class AddSigningKeysAndTokenPolicies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsumedRefreshTokens",
                columns: table => new
                {
                    TokenId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConsumedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumedRefreshTokens", x => x.TokenId);
                });

            migrationBuilder.CreateTable(
                name: "SigningKeyRingEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kid = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetiredUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotBeforeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotAfterUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Algorithm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    KeyType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublicJwkJson = table.Column<string>(type: "text", nullable: false),
                    PrivateKeyProtected = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SigningKeyRingEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenPolicyOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicAccessTokenMinutes = table.Column<int>(type: "integer", nullable: true),
                    PublicIdentityTokenMinutes = table.Column<int>(type: "integer", nullable: true),
                    PublicRefreshTokenAbsoluteDays = table.Column<int>(type: "integer", nullable: true),
                    PublicRefreshTokenSlidingDays = table.Column<int>(type: "integer", nullable: true),
                    ConfidentialAccessTokenMinutes = table.Column<int>(type: "integer", nullable: true),
                    ConfidentialIdentityTokenMinutes = table.Column<int>(type: "integer", nullable: true),
                    ConfidentialRefreshTokenAbsoluteDays = table.Column<int>(type: "integer", nullable: true),
                    ConfidentialRefreshTokenSlidingDays = table.Column<int>(type: "integer", nullable: true),
                    RefreshRotationEnabled = table.Column<bool>(type: "boolean", nullable: true),
                    RefreshReuseDetectionEnabled = table.Column<bool>(type: "boolean", nullable: true),
                    RefreshReuseLeewaySeconds = table.Column<int>(type: "integer", nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenPolicyOverrides", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedRefreshTokens_ExpiresUtc",
                table: "ConsumedRefreshTokens",
                column: "ExpiresUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SigningKeyRingEntries_Kid",
                table: "SigningKeyRingEntries",
                column: "Kid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SigningKeyRingEntries_Status",
                table: "SigningKeyRingEntries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TokenPolicyOverrides_UpdatedUtc",
                table: "TokenPolicyOverrides",
                column: "UpdatedUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumedRefreshTokens");

            migrationBuilder.DropTable(
                name: "SigningKeyRingEntries");

            migrationBuilder.DropTable(
                name: "TokenPolicyOverrides");
        }
    }
}
