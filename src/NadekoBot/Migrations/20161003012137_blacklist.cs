using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Migrations
{
    public partial class blacklist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "BlacklistItem",
                nullable: false,
                defaultValue: BlacklistItem.BlacklistType.Server);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "BlacklistItem");
        }
    }
}
