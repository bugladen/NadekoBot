using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class cleanup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"DELETE FROM GuildRepeater
WHERE GuildConfigId is null;

DELETE FROM AntiSpamIgnore
WHERE AntiSpamSettingId is null;

DELETE FROM BlacklistItem
WHERE BotConfigId is null;

DELETE FROM CommandAlias
WHERE GuildConfigId is null;

DELETE FROM CommandCooldown
WHERE GuildConfigId is null;

DELETE FROM Currency
WHERE UserId <= 12345679987654321;

DELETE FROM CustomReactions
WHERE GuildId='' or GuildId is null;

DELETE FROM DelMsgOnCmdChannel
WHERE GuildConfigId is null or ChannelId < 1000;

DELETE FROM BlockedCmdOrMdl
WHERE BotConfigId is null and BotConfigId1 is null;

DELETE FROM ExcludedItem
WHERE XpSettingsId is null;

DELETE FROM FilterChannelId 
WHERE GuildConfigId is null and GuildConfigId1 is null;

DELETE FROM FilteredWord
WHERE GuildConfigId is null;

DELETE FROM FollowedStream
WHERE GuildConfigId is null;

DELETE FROM GCChannelId
WHERE GuildConfigId is null;

DELETE FROM GroupName
WHERE GuildConfigId is null;

DELETE FROM MutedUserId
WHERE GuildConfigId is null;

DELETE FROM NsfwBlacklitedTag
WHERE GuildConfigId is null;

DELETE FROM Permissionv2
WHERE GuildConfigId is null;

DELETE FROM PlayingStatus
WHERE BotConfigId is null;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
