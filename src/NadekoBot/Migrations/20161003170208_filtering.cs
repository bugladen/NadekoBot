using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class filtering : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FilterChannelId",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    ChannelId = table.Column<ulong>(nullable: false),
                    GuildConfigId = table.Column<int>(nullable: true),
                    GuildConfigId1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterChannelId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilterChannelId_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FilterChannelId_GuildConfigs_GuildConfigId1",
                        column: x => x.GuildConfigId1,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FilteredWord",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    GuildConfigId = table.Column<int>(nullable: true),
                    Word = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilteredWord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilteredWord_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddColumn<bool>(
                name: "FilterInvites",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FilterWords",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_FilterChannelId_GuildConfigId",
                table: "FilterChannelId",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_FilterChannelId_GuildConfigId1",
                table: "FilterChannelId",
                column: "GuildConfigId1");

            migrationBuilder.CreateIndex(
                name: "IX_FilteredWord_GuildConfigId",
                table: "FilteredWord",
                column: "GuildConfigId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilterInvites",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "FilterWords",
                table: "GuildConfigs");

            migrationBuilder.DropTable(
                name: "FilterChannelId");

            migrationBuilder.DropTable(
                name: "FilteredWord");
        }
    }
}
