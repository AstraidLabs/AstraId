using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthServer.Migrations
{
    public partial class EmailOutboxAndInactivityNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastInactivityWarningSentUtc",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDeletionUtc",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmailOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAttemptUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ToEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ToName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    TextBody = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailOutboxMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailOutboxMessages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InactivityPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    WarningAfterDays = table.Column<int>(type: "integer", nullable: false),
                    DeactivateAfterDays = table.Column<int>(type: "integer", nullable: false),
                    DeleteAfterDays = table.Column<int>(type: "integer", nullable: false),
                    WarningRepeatDays = table.Column<int>(type: "integer", nullable: true),
                    DeleteMode = table.Column<int>(type: "integer", nullable: false),
                    ProtectAdmins = table.Column<bool>(type: "boolean", nullable: false),
                    ProtectedRoles = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InactivityPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InactivityPolicies_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_EmailOutboxMessages_IdempotencyKey", table: "EmailOutboxMessages", column: "IdempotencyKey", unique: true, filter: "\"IdempotencyKey\" IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_EmailOutboxMessages_Status_NextAttemptUtc", table: "EmailOutboxMessages", columns: new[] { "Status", "NextAttemptUtc" });
            migrationBuilder.CreateIndex(name: "IX_EmailOutboxMessages_UserId_CreatedUtc", table: "EmailOutboxMessages", columns: new[] { "UserId", "CreatedUtc" });
            migrationBuilder.CreateIndex(name: "IX_InactivityPolicies_UpdatedByUserId", table: "InactivityPolicies", column: "UpdatedByUserId");
            migrationBuilder.CreateIndex(name: "IX_InactivityPolicies_UpdatedUtc", table: "InactivityPolicies", column: "UpdatedUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EmailOutboxMessages");
            migrationBuilder.DropTable(name: "InactivityPolicies");
            migrationBuilder.DropColumn(name: "LastInactivityWarningSentUtc", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "ScheduledDeletionUtc", table: "AspNetUsers");
        }
    }
}
