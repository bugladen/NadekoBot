using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class logging : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropIndex(
            //    name: "IX_GuildConfigs_LogSettingId",
            //    table: "GuildConfigs");

            //migrationBuilder.DropIndex(
            //    name: "IX_IgnoredLogChannels_LogSettingId",
            //    table: "IgnoredLogChannels");

            //migrationBuilder.DropIndex(
            //    name: "IX_IgnoredVoicePresenceChannels_LogSettingId",
            //    table: "IgnoredVoicePresenceChannels");

            //migrationBuilder.DropTable("LogSettings");
            //migrationBuilder.DropTable("IgnoredLogChannels");
            //migrationBuilder.DropTable("IgnoredVoicePresenceChannels");

            //migrationBuilder.CreateTable(
            //    name: "LogSettings",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(nullable: false)
            //            .Annotation("Autoincrement", true),
            //        LogOtherId = table.Column<ulong>(nullable: true),
            //        MessageUpdatedId = table.Column<ulong>(nullable: true),
            //        MessageDeletedId = table.Column<ulong>(nullable: true),
            //        UserJoinedId = table.Column<ulong>(nullable: true),
            //        UserLeftId = table.Column<ulong>(nullable: true),
            //        UserBannedId = table.Column<ulong>(nullable: true),
            //        UserUnbannedId = table.Column<ulong>(nullable: true),
            //        UserUpdatedId = table.Column<ulong>(nullable: true),
            //        ChannelCreatedId = table.Column<ulong>(nullable: true),
            //        ChannelDestroyedId = table.Column<ulong>(nullable: true),
            //        ChannelUpdatedId = table.Column<ulong>(nullable: true),
            //        LogUserPresenceId = table.Column<ulong>(nullable: true),
            //        LogVoicePresenceId = table.Column<ulong>(nullable: true),
            //        LogVoicePresenceTTSId = table.Column<ulong>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_LogSettings", x => x.Id);
            //    });

            //migrationBuilder.CreateTable(
            //     name: "IgnoredLogChannels",
            //     columns: table => new
            //     {
            //         Id = table.Column<int>(nullable: false)
            //             .Annotation("Autoincrement", true),
            //         ChannelId = table.Column<ulong>(nullable: false),
            //         LogSettingId = table.Column<int>(nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_IgnoredLogChannels", x => x.Id);
            //         table.ForeignKey(
            //             name: "FK_IgnoredLogChannels_LogSettings_LogSettingId",
            //             column: x => x.LogSettingId,
            //             principalTable: "LogSettings",
            //             principalColumn: "Id",
            //             onDelete: ReferentialAction.Restrict);
            //     });

            //migrationBuilder.CreateTable(
            //    name: "IgnoredVoicePresenceChannels",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(nullable: false)
            //            .Annotation("Autoincrement", true),
            //        ChannelId = table.Column<ulong>(nullable: false),
            //        LogSettingId = table.Column<int>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_IgnoredVoicePresenceChannels", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_IgnoredVoicePresenceChannels_LogSettings_LogSettingId",
            //            column: x => x.LogSettingId,
            //            principalTable: "LogSettings",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_GuildConfigs_LogSettingId",
            //    table: "GuildConfigs",
            //    column: "LogSettingId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_IgnoredLogChannels_LogSettingId",
            //    table: "IgnoredLogChannels",
            //    column: "LogSettingId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_IgnoredVoicePresenceChannels_LogSettingId",
            //    table: "IgnoredVoicePresenceChannels",
            //    column: "LogSettingId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
