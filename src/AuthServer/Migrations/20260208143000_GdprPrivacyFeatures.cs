using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthServer.Migrations
{
    public partial class GdprPrivacyFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnteredIdentifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReasonCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginHistory_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PrivacyPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginHistoryRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    ErrorLogRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    TokenRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    AuditLogRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    DeletionCooldownDays = table.Column<int>(type: "integer", nullable: false),
                    AnonymizeInsteadOfHardDelete = table.Column<bool>(type: "boolean", nullable: false),
                    RequireMfaForDeletionRequest = table.Column<bool>(type: "boolean", nullable: false),
                    RequireRecentReauthForExport = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivacyPolicies_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DeletionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ExecutedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CooldownUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeletionRequests_AspNetUsers_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeletionRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_DeletionRequests_ApprovedBy", table: "DeletionRequests", column: "ApprovedBy");
            migrationBuilder.CreateIndex(name: "IX_DeletionRequests_UserId_RequestedUtc", table: "DeletionRequests", columns: new[] { "UserId", "RequestedUtc" });
            migrationBuilder.CreateIndex(name: "IX_LoginHistory_TimestampUtc", table: "LoginHistory", column: "TimestampUtc");
            migrationBuilder.CreateIndex(name: "IX_LoginHistory_UserId_TimestampUtc", table: "LoginHistory", columns: new[] { "UserId", "TimestampUtc" });
            migrationBuilder.CreateIndex(name: "IX_PrivacyPolicies_UpdatedByUserId", table: "PrivacyPolicies", column: "UpdatedByUserId");
            migrationBuilder.CreateIndex(name: "IX_PrivacyPolicies_UpdatedUtc", table: "PrivacyPolicies", column: "UpdatedUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DeletionRequests");
            migrationBuilder.DropTable(name: "LoginHistory");
            migrationBuilder.DropTable(name: "PrivacyPolicies");
        }
    }
}
