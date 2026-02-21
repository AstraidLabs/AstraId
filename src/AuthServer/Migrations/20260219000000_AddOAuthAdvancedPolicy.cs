using System;
using AuthServer.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthServer.Migrations
{
    public partial class AddOAuthAdvancedPolicy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OAuthAdvancedPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceFlowEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DeviceFlowUserCodeTtlMinutes = table.Column<int>(type: "integer", nullable: false),
                    DeviceFlowPollingIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    TokenExchangeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TokenExchangeAllowedClientIdsJson = table.Column<string>(type: "text", nullable: false),
                    TokenExchangeAllowedAudiencesJson = table.Column<string>(type: "text", nullable: false),
                    RefreshRotationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RefreshReuseDetectionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RefreshReuseAction = table.Column<int>(type: "integer", nullable: false),
                    BackChannelLogoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FrontChannelLogoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LogoutTokenTtlMinutes = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "''::bytea")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthAdvancedPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OAuthAdvancedPolicies_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAdvancedPolicies_UpdatedAtUtc",
                table: "OAuthAdvancedPolicies",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAdvancedPolicies_UpdatedByUserId",
                table: "OAuthAdvancedPolicies",
                column: "UpdatedByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OAuthAdvancedPolicies");
        }
    }
}
