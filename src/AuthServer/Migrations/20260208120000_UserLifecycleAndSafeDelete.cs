using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthServer.Migrations
{
    public partial class UserLifecycleAndSafeDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AnonymizedUtc",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedUtc",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymized",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedDeletionUtc",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserActivities",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastPasswordChangeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivities", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserActivities_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLifecyclePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    DeactivateAfterDays = table.Column<int>(type: "integer", nullable: false),
                    DeleteAfterDays = table.Column<int>(type: "integer", nullable: false),
                    HardDeleteAfterDays = table.Column<int>(type: "integer", nullable: true),
                    HardDeleteEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WarnBeforeLogoutMinutes = table.Column<int>(type: "integer", nullable: false),
                    IdleLogoutMinutes = table.Column<int>(type: "integer", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLifecyclePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLifecyclePolicies_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivities_LastSeenUtc",
                table: "UserActivities",
                column: "LastSeenUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserLifecyclePolicies_UpdatedByUserId",
                table: "UserLifecyclePolicies",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_ActorUserId",
                table: "ErrorLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenIncidents_ActorUserId",
                table: "TokenIncidents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenIncidents_UserId",
                table: "TokenIncidents",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ErrorLogs_AspNetUsers_ActorUserId",
                table: "ErrorLogs",
                column: "ActorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TokenIncidents_AspNetUsers_ActorUserId",
                table: "TokenIncidents",
                column: "ActorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TokenIncidents_AspNetUsers_UserId",
                table: "TokenIncidents",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSecurityEvents_AspNetUsers_UserId",
                table: "UserSecurityEvents",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_AuditLogs_AspNetUsers_ActorUserId", table: "AuditLogs");
            migrationBuilder.DropForeignKey(name: "FK_ErrorLogs_AspNetUsers_ActorUserId", table: "ErrorLogs");
            migrationBuilder.DropForeignKey(name: "FK_TokenIncidents_AspNetUsers_ActorUserId", table: "TokenIncidents");
            migrationBuilder.DropForeignKey(name: "FK_TokenIncidents_AspNetUsers_UserId", table: "TokenIncidents");
            migrationBuilder.DropForeignKey(name: "FK_UserSecurityEvents_AspNetUsers_UserId", table: "UserSecurityEvents");

            migrationBuilder.DropTable(name: "UserActivities");
            migrationBuilder.DropTable(name: "UserLifecyclePolicies");

            migrationBuilder.DropIndex(name: "IX_AuditLogs_ActorUserId", table: "AuditLogs");
            migrationBuilder.DropIndex(name: "IX_ErrorLogs_ActorUserId", table: "ErrorLogs");
            migrationBuilder.DropIndex(name: "IX_TokenIncidents_ActorUserId", table: "TokenIncidents");
            migrationBuilder.DropIndex(name: "IX_TokenIncidents_UserId", table: "TokenIncidents");

            migrationBuilder.DropColumn(name: "AnonymizedUtc", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "DeactivatedUtc", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "IsAnonymized", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "RequestedDeletionUtc", table: "AspNetUsers");
        }
    }
}
