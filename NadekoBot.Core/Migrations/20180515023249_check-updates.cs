using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace NadekoBot.Migrations
{
    public partial class checkupdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CheckForUpdates",
                table: "BotConfig",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdate",
                table: "BotConfig",
                nullable: false,
                defaultValue: new DateTime(2018, 5, 5, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<string>(
                name: "UpdateString",
                table: "BotConfig",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckForUpdates",
                table: "BotConfig");

            migrationBuilder.DropColumn(
                name: "LastUpdate",
                table: "BotConfig");

            migrationBuilder.DropColumn(
                name: "UpdateString",
                table: "BotConfig");
        }
    }
}
