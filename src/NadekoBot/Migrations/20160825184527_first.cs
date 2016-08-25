using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class first : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClashOfClans",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    ChannelId = table.Column<ulong>(nullable: false),
                    EnemyClan = table.Column<string>(nullable: true),
                    GuildId = table.Column<ulong>(nullable: false),
                    Size = table.Column<int>(nullable: false),
                    StartedAt = table.Column<DateTime>(nullable: false),
                    WarState = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClashOfClans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Donators",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    Amount = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    UserId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    AutoAssignRoleId = table.Column<ulong>(nullable: false),
                    AutoDeleteByeMessages = table.Column<bool>(nullable: false),
                    AutoDeleteGreetMessages = table.Column<bool>(nullable: false),
                    AutoDeleteGreetMessagesTimer = table.Column<int>(nullable: false),
                    ByeMessageChannelId = table.Column<ulong>(nullable: false),
                    ChannelByeMessageText = table.Column<string>(nullable: true),
                    ChannelGreetMessageText = table.Column<string>(nullable: true),
                    DeleteMessageOnCommand = table.Column<bool>(nullable: false),
                    DmGreetMessageText = table.Column<string>(nullable: true),
                    GreetMessageChannelId = table.Column<ulong>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    SendChannelByeMessage = table.Column<bool>(nullable: false),
                    SendChannelGreetMessage = table.Column<bool>(nullable: false),
                    SendDmGreetMessage = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    AuthorId = table.Column<ulong>(nullable: false),
                    AuthorName = table.Column<string>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    Keyword = table.Column<string>(nullable: false),
                    Text = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    ChannelId = table.Column<ulong>(nullable: false),
                    IsPrivate = table.Column<bool>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    ServerId = table.Column<ulong>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    When = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClashCallers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    BaseDestroyed = table.Column<bool>(nullable: false),
                    CallUser = table.Column<string>(nullable: true),
                    ClashWarId = table.Column<int>(nullable: false),
                    Stars = table.Column<int>(nullable: false),
                    TimeAdded = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClashCallers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClashCallers_ClashOfClans_ClashWarId",
                        column: x => x.ClashWarId,
                        principalTable: "ClashOfClans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClashCallers_ClashWarId",
                table: "ClashCallers",
                column: "ClashWarId");

            migrationBuilder.CreateIndex(
                name: "IX_Donators_UserId",
                table: "Donators",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildConfigs_GuildId",
                table: "GuildConfigs",
                column: "GuildId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClashCallers");

            migrationBuilder.DropTable(
                name: "Donators");

            migrationBuilder.DropTable(
                name: "GuildConfigs");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "Reminders");

            migrationBuilder.DropTable(
                name: "ClashOfClans");
        }
    }
}
