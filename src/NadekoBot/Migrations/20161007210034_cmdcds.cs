using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class cmdcds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommandCooldown",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    CommandName = table.Column<string>(nullable: true),
                    GuildConfigId = table.Column<int>(nullable: true),
                    Seconds = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandCooldown", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandCooldown_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommandCooldown_GuildConfigId",
                table: "CommandCooldown",
                column: "GuildConfigId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandCooldown");
        }
    }
}
