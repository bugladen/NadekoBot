using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class moarperms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permission_GuildConfigs_GuildConfigId",
                table: "Permission");

            migrationBuilder.DropIndex(
                name: "IX_Permission_GuildConfigId",
                table: "Permission");

            migrationBuilder.DropColumn(
                name: "GuildConfigId",
                table: "Permission");

            migrationBuilder.AddColumn<int>(
                name: "NextId",
                table: "Permission",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RootPermissionId",
                table: "GuildConfigs",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permission_NextId",
                table: "Permission",
                column: "NextId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildConfigs_RootPermissionId",
                table: "GuildConfigs",
                column: "RootPermissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_GuildConfigs_Permission_RootPermissionId",
                table: "GuildConfigs",
                column: "RootPermissionId",
                principalTable: "Permission",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Permission_Permission_NextId",
                table: "Permission",
                column: "NextId",
                principalTable: "Permission",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuildConfigs_Permission_RootPermissionId",
                table: "GuildConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_Permission_Permission_NextId",
                table: "Permission");

            migrationBuilder.DropIndex(
                name: "IX_Permission_NextId",
                table: "Permission");

            migrationBuilder.DropIndex(
                name: "IX_GuildConfigs_RootPermissionId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "NextId",
                table: "Permission");

            migrationBuilder.DropColumn(
                name: "RootPermissionId",
                table: "GuildConfigs");

            migrationBuilder.AddColumn<int>(
                name: "GuildConfigId",
                table: "Permission",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permission_GuildConfigId",
                table: "Permission",
                column: "GuildConfigId");

            migrationBuilder.AddForeignKey(
                name: "FK_Permission_GuildConfigs_GuildConfigId",
                table: "Permission",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
