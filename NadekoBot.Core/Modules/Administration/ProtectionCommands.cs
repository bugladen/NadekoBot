using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Common;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        // todo server error log
        [Group]
        public class ProtectionCommands : NadekoSubmodule<ProtectionService>
        {
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public Task AntiRaid()
            {
                if(_service.TryStopAntiRaid(Context.Guild.Id))
                {
                    return ReplyConfirmLocalized("prot_disable", "Anti-Raid");
                }
                else
                {
                    return ReplyErrorLocalized("anti_raid_not_running");
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task AntiRaid(int userThreshold, int seconds = 10, PunishmentAction action = PunishmentAction.Mute)
            {
                if (userThreshold < 2 || userThreshold > 30)
                {
                    await ReplyErrorLocalized("raid_cnt", 2, 30).ConfigureAwait(false);
                    return;
                }

                if (seconds < 2 || seconds > 300)
                {
                    await ReplyErrorLocalized("raid_time", 2, 300).ConfigureAwait(false);
                    return;
                }

                var stats = await _service.StartAntiRaidAsync(Context.Guild.Id, userThreshold, seconds, action);

                await Context.Channel.SendConfirmAsync(GetText("prot_enable", "Anti-Raid"), $"{Context.User.Mention} {GetAntiRaidString(stats)}")
                        .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [Priority(1)]
            public Task AntiSpam()
            {
                if(_service.TryStopAntiSpam(Context.Guild.Id))
                {
                    return ReplyConfirmLocalized("prot_disable", "Anti-Spam");
                }
                else
                {
                    return ReplyErrorLocalized("anti_spam_not_running");
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [Priority(0)]
            public async Task AntiSpam(int messageCount, PunishmentAction action = PunishmentAction.Mute, int time = 0)
            {
                if (messageCount < 2 || messageCount > 10)
                    return;

                if (time < 0 || time > 60 * 12)
                    return;

                var stats = await _service.StartAntiSpamAsync(Context.Guild.Id, messageCount, time, action);

                await Context.Channel.SendConfirmAsync(GetText("prot_enable", "Anti-Spam"), 
                    $"{Context.User.Mention} {GetAntiSpamString(stats)}").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task AntispamIgnore()
            {
                var added = await _service.AntiSpamIgnoreAsync(Context.Guild.Id, Context.Channel.Id);

                await ReplyConfirmLocalized(added ? "spam_ignore" : "spam_not_ignore", "Anti-Spam").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AntiList()
            {
                var (spam, raid) = _service.GetAntiStats(Context.Guild.Id);

                if (spam == null && raid == null)
                {
                    await ReplyConfirmLocalized("prot_none").ConfigureAwait(false);
                    return;
                }

                var embed = new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("prot_active"));

                if (spam != null)
                    embed.AddField(efb => efb.WithName("Anti-Spam")
                        .WithValue(GetAntiSpamString(spam))
                        .WithIsInline(true));

                if (raid != null)
                    embed.AddField(efb => efb.WithName("Anti-Raid")
                        .WithValue(GetAntiRaidString(raid))
                        .WithIsInline(true));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }


            private string GetAntiSpamString(AntiSpamStats stats)
            {
                var settings = stats.AntiSpamSettings;
                var ignoredString = string.Join(", ", settings.IgnoredChannels.Select(c => $"<#{c.ChannelId}>"));

                if (string.IsNullOrWhiteSpace(ignoredString))
                    ignoredString = "none";

                string add = "";
                if (settings.Action == PunishmentAction.Mute
                    && settings.MuteTime > 0)
                {
                    add = " (" + settings.MuteTime + "s)";
                }

                return GetText("spam_stats",
                        Format.Bold(settings.MessageThreshold.ToString()),
                        Format.Bold(settings.Action.ToString() + add),
                        ignoredString);
            }

            private string GetAntiRaidString(AntiRaidStats stats) => GetText("raid_stats",
                Format.Bold(stats.AntiRaidSettings.UserThreshold.ToString()),
                Format.Bold(stats.AntiRaidSettings.Seconds.ToString()),
                Format.Bold(stats.AntiRaidSettings.Action.ToString()));
        }
    }
}