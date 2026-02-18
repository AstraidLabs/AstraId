using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppServer.Migrations;

public partial class AddGameModule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GameEventStates",
            columns: table => new
            {
                PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                ActiveEventsJson = table.Column<string>(type: "text", nullable: false),
                PendingEventsJson = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_GameEventStates", x => x.PlayerId); });

        migrationBuilder.CreateTable(
            name: "GamePlayers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserSub = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Phase = table.Column<int>(type: "integer", nullable: false),
                PrivateSeed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                SharedShardId = table.Column<Guid>(type: "uuid", nullable: true),
                LastTickUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ShieldUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RowVersion = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_GamePlayers", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "GameResourceStates",
            columns: table => new
            {
                PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                Energy = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                Minerals = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                Alloys = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                Research = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                Influence = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                Unity = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                StorageCapsJson = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_GameResourceStates", x => x.PlayerId); });

        migrationBuilder.CreateTable(
            name: "GameResearchStates",
            columns: table => new
            {
                PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                ActiveProjectId = table.Column<string>(type: "text", nullable: true),
                Progress = table.Column<decimal>(type: "numeric", nullable: false),
                CompletedJson = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_GameResearchStates", x => x.PlayerId); });

        migrationBuilder.CreateTable(
            name: "GameSharedGalaxies",
            columns: table => new
            {
                ShardId = table.Column<Guid>(type: "uuid", nullable: false),
                Seed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                GenerationVersion = table.Column<int>(type: "integer", nullable: false),
                ParamsJson = table.Column<string>(type: "text", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_GameSharedGalaxies", x => x.ShardId); });

        migrationBuilder.CreateTable(
            name: "GameCommands",
            columns: table => new
            {
                CommandId = table.Column<Guid>(type: "uuid", nullable: false),
                PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                PayloadJson = table.Column<string>(type: "text", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ProcessedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                ResultJson = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_GameCommands", x => x.CommandId); });

        migrationBuilder.CreateTable(
            name: "GameSystemStates",
            columns: table => new
            {
                PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                SystemId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                DiscoveryState = table.Column<int>(type: "integer", nullable: false),
                Owned = table.Column<bool>(type: "boolean", nullable: false),
                SurveyProgress = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_GameSystemStates", x => new { x.PlayerId, x.SystemId }); });

        migrationBuilder.CreateTable(
            name: "GamePlanetStates",
            columns: table => new
            {
                PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                SystemId = table.Column<string>(type: "text", nullable: false),
                PlanetIndex = table.Column<int>(type: "integer", nullable: false),
                Colonized = table.Column<bool>(type: "boolean", nullable: false),
                Pop = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                BuildingsJson = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_GamePlanetStates", x => new { x.PlayerId, x.SystemId, x.PlanetIndex }); });

        migrationBuilder.CreateIndex(name: "IX_GameCommands_CommandId", table: "GameCommands", column: "CommandId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_GameCommands_PlayerId_CreatedUtc", table: "GameCommands", columns: new[] { "PlayerId", "CreatedUtc" });
        migrationBuilder.CreateIndex(name: "IX_GamePlayers_UserSub", table: "GamePlayers", column: "UserSub", unique: true);
        migrationBuilder.CreateIndex(name: "IX_GameSystemStates_PlayerId_SystemId", table: "GameSystemStates", columns: new[] { "PlayerId", "SystemId" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("GameCommands");
        migrationBuilder.DropTable("GameEventStates");
        migrationBuilder.DropTable("GamePlanetStates");
        migrationBuilder.DropTable("GamePlayers");
        migrationBuilder.DropTable("GameResourceStates");
        migrationBuilder.DropTable("GameResearchStates");
        migrationBuilder.DropTable("GameSharedGalaxies");
        migrationBuilder.DropTable("GameSystemStates");
    }
}
