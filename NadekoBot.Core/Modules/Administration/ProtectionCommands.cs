using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Common;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class ProtectionCommands : NadekoSubmodule<ProtectionService>
        {
            private readonly MuteService _mute;
            private readonly DbService _db;

            public ProtectionCommands(MuteService mute, DbService db)
            {
                _mute = mute;
                _db = db;
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

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task AntiRaid(int userThreshold = 5, int seconds = 10, PunishmentAction action = PunishmentAction.Mute)
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

                if (_service.AntiRaidGuilds.TryRemove(Context.Guild.Id, out _))
                {
                    using (var uow = _db.UnitOfWork)
                    {
                        var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiRaidSetting));

                        gc.AntiRaidSetting = null;
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    await ReplyConfirmLocalized("prot_disable", "Anti-Raid").ConfigureAwait(false);
                    return;
                }

                try
                {
                    await _mute.GetMuteRole(Context.Guild).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    await ReplyErrorLocalized("prot_error").ConfigureAwait(false);
                    return;
                }

                var stats = new AntiRaidStats()
                {
                    AntiRaidSettings = new AntiRaidSetting()
                    {
                        Action = action,
                        Seconds = seconds,
                        UserThreshold = userThreshold,
                    }
                };

                _service.AntiRaidGuilds.AddOrUpdate(Context.Guild.Id, stats, (key, old) => stats);

                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiRaidSetting));

                    gc.AntiRaidSetting = stats.AntiRaidSettings;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await Context.Channel.SendConfirmAsync(GetText("prot_enable", "Anti-Raid"), $"{Context.User.Mention} {GetAntiRaidString(stats)}")
                        .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [Priority(1)]
            public async Task AntiSpam()
            {
                if (_service.AntiSpamGuilds.TryRemove(Context.Guild.Id, out var removed))
                {
                    removed.UserStats.ForEach(x => x.Value.Dispose());
                    using (var uow = _db.UnitOfWork)
                    {
                        var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting)
                            .ThenInclude(x => x.IgnoredChannels));

                        gc.AntiSpamSetting = null;
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    await ReplyConfirmLocalized("prot_disable", "Anti-Spam").ConfigureAwait(false);
                    return;
                }

                await AntiSpam(3).ConfigureAwait(false);
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

                try
                {
                    await _mute.GetMuteRole(Context.Guild).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    await ReplyErrorLocalized("prot_error").ConfigureAwait(false);
                    return;
                }

                var stats = new AntiSpamStats
                {
                    AntiSpamSettings = new AntiSpamSetting()
                    {
                        Action = action,
                        MessageThreshold = messageCount,
                        MuteTime = time,
                    }
                };

                stats = _service.AntiSpamGuilds.AddOrUpdate(Context.Guild.Id, stats, (key, old) =>
                {
                    stats.AntiSpamSettings.IgnoredChannels = old.AntiSpamSettings.IgnoredChannels;
                    return stats;
                });

                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting));

                    if (gc.AntiSpamSetting != null)
                    {
                        gc.AntiSpamSetting.Action = stats.AntiSpamSettings.Action;
                        gc.AntiSpamSetting.MessageThreshold = stats.AntiSpamSettings.MessageThreshold;
                        gc.AntiSpamSetting.MuteTime = stats.AntiSpamSettings.MuteTime;
                    }
                    else
                    {
                        gc.AntiSpamSetting = stats.AntiSpamSettings;
                    }
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await Context.Channel.SendConfirmAsync(GetText("prot_enable", "Anti-Spam"), $"{Context.User.Mention} {GetAntiSpamString(stats)}").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task AntispamIgnore()
            {
                var channel = (ITextChannel)Context.Channel;

                var obj = new AntiSpamIgnore()
                {
                    ChannelId = channel.Id
                };
                bool added;
                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting).ThenInclude(x => x.IgnoredChannels));
                    var spam = gc.AntiSpamSetting;
                    if (spam == null)
                    {
                        return;
                    }

                    if (spam.IgnoredChannels.Add(obj))
                    {
                        if (_service.AntiSpamGuilds.TryGetValue(Context.Guild.Id, out var temp))
                            temp.AntiSpamSettings.IgnoredChannels.Add(obj);
                        added = true;
                    }
                    else
                    {
                        spam.IgnoredChannels.Remove(obj);
                        if (_service.AntiSpamGuilds.TryGetValue(Context.Guild.Id, out var temp))
                            temp.AntiSpamSettings.IgnoredChannels.Remove(obj);
                        added = false;
                    }

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await ReplyConfirmLocalized(added ? "spam_ignore" : "spam_not_ignore", "Anti-Spam").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AntiList()
            {
                _service.AntiSpamGuilds.TryGetValue(Context.Guild.Id, out var spam);

                _service.AntiRaidGuilds.TryGetValue(Context.Guild.Id, out var raid);

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
        }
    }
}