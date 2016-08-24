using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class FirstMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    DeleteMessageOnCommand = table.Column<bool>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configs", x => x.Id);
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

            migrationBuilder.CreateIndex(
                name: "IX_Configs_GuildId",
                table: "Configs",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Donators_UserId",
                table: "Donators",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configs");

            migrationBuilder.DropTable(
                name: "Donators");

            migrationBuilder.DropTable(
                name: "Quotes");
        }
    }
}
