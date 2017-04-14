using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        public enum ProtectionType
        {
            Raiding,
            Spamming,
        }

        public class AntiRaidStats
        {
            public AntiRaidSetting AntiRaidSettings { get; set; }
            public int UsersCount { get; set; }
            public ConcurrentHashSet<IGuildUser> RaidUsers { get; set; } = new ConcurrentHashSet<IGuildUser>();
        }

        public class AntiSpamStats
        {
            public AntiSpamSetting AntiSpamSettings { get; set; }
            public ConcurrentDictionary<ulong, UserSpamStats> UserStats { get; set; }
                = new ConcurrentDictionary<ulong, UserSpamStats>();
        }

        public class UserSpamStats
        {
            public int Count { get; set; }
            public string LastMessage { get; set; }

            public UserSpamStats(string msg)
            {
                Count = 1;
                LastMessage = msg.ToUpperInvariant();
            }

            public void ApplyNextMessage(IUserMessage message)
            {
                var upperMsg = message.Content.ToUpperInvariant();
                if (upperMsg != LastMessage || (string.IsNullOrWhiteSpace(upperMsg) && message.Attachments.Any()))
                {
                    LastMessage = upperMsg;
                    Count = 0;
                }
                else
                {
                    Count++;
                }
            }
        }

        [Group]
        public class ProtectionCommands : NadekoSubmodule
        {
            private static readonly ConcurrentDictionary<ulong, AntiRaidStats> _antiRaidGuilds =
                    new ConcurrentDictionary<ulong, AntiRaidStats>();
            // guildId | (userId|messages)
            private static readonly ConcurrentDictionary<ulong, AntiSpamStats> _antiSpamGuilds =
                    new ConcurrentDictionary<ulong, AntiSpamStats>();

            private new static readonly Logger _log;

            static ProtectionCommands()
            {
                _log = LogManager.GetCurrentClassLogger();

                foreach (var gc in NadekoBot.AllGuildConfigs)
                {
                    var raid = gc.AntiRaidSetting;
                    var spam = gc.AntiSpamSetting;

                    if (raid != null)
                    {
                        var raidStats = new AntiRaidStats() { AntiRaidSettings = raid };
                        _antiRaidGuilds.TryAdd(gc.GuildId, raidStats);
                    }

                    if (spam != null)
                        _antiSpamGuilds.TryAdd(gc.GuildId, new AntiSpamStats() { AntiSpamSettings = spam });
                }

                NadekoBot.Client.MessageReceived += (imsg) =>
                {
                    var msg = imsg as IUserMessage;
                    if (msg == null || msg.Author.IsBot)
                        return Task.CompletedTask;

                    var channel = msg.Channel as ITextChannel;
                    if (channel == null)
                        return Task.CompletedTask;
                    var _ = Task.Run(async () =>
                    {
                        try
                        {
                            AntiSpamStats spamSettings;
                            if (!_antiSpamGuilds.TryGetValue(channel.Guild.Id, out spamSettings) ||
                                spamSettings.AntiSpamSettings.IgnoredChannels.Contains(new AntiSpamIgnore()
                                {
                                    ChannelId = channel.Id
                                }))
                                return;

                            var stats = spamSettings.UserStats.AddOrUpdate(msg.Author.Id, new UserSpamStats(msg.Content),
                                (id, old) =>
                                {
                                    old.ApplyNextMessage(msg); return old;
                                });

                            if (stats.Count >= spamSettings.AntiSpamSettings.MessageThreshold)
                            {
                                if (spamSettings.UserStats.TryRemove(msg.Author.Id, out stats))
                                {
                                    await PunishUsers(spamSettings.AntiSpamSettings.Action, ProtectionType.Spamming, (IGuildUser)msg.Author)
                                        .ConfigureAwait(false);
                                }
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    });
                    return Task.CompletedTask;
                };

                NadekoBot.Client.UserJoined += (usr) =>
                {
                    if (usr.IsBot)
                        return Task.CompletedTask;
                    AntiRaidStats settings;
                    if (!_antiRaidGuilds.TryGetValue(usr.Guild.Id, out settings))
                        return Task.CompletedTask;
                    if (!settings.RaidUsers.Add(usr))
                        return Task.CompletedTask;

                    var _ = Task.Run(async () =>
                    {
                        try
                        {
                            ++settings.UsersCount;

                            if (settings.UsersCount >= settings.AntiRaidSettings.UserThreshold)
                            {
                                var users = settings.RaidUsers.ToArray();
                                settings.RaidUsers.Clear();

                                await PunishUsers(settings.AntiRaidSettings.Action, ProtectionType.Raiding, users).ConfigureAwait(false);
                            }
                            await Task.Delay(1000 * settings.AntiRaidSettings.Seconds).ConfigureAwait(false);

                            settings.RaidUsers.TryRemove(usr);
                            --settings.UsersCount;

                        }
                        catch
                        {
                            // ignored
                        }
                    });
                    return Task.CompletedTask;
                };
            }

            private static async Task PunishUsers(PunishmentAction action, ProtectionType pt, params IGuildUser[] gus)
            {
                _log.Info($"[{pt}] - Punishing [{gus.Length}] users with [{action}] in {gus[0].Guild.Name} guild");
                foreach (var gu in gus)
                {
                    switch (action)
                    {
                        case PunishmentAction.Mute:
                            try
                            {
                                await MuteCommands.MuteUser(gu).ConfigureAwait(false);
                            }
                            catch (Exception ex) { _log.Warn(ex, "I can't apply punishement"); }
                            break;
                        case PunishmentAction.Kick:
                            try
                            {
                                await gu.KickAsync().ConfigureAwait(false);
                            }
                            catch (Exception ex) { _log.Warn(ex, "I can't apply punishement"); }
                            break;
                        case PunishmentAction.Softban:
                            try
                            {
                                await gu.Guild.AddBanAsync(gu, 7).ConfigureAwait(false);
                                try
                                {
                                    await gu.Guild.RemoveBanAsync(gu).ConfigureAwait(false);
                                }
                                catch
                                {
                                    await gu.Guild.RemoveBanAsync(gu).ConfigureAwait(false);
                                    // try it twice, really don't want to ban user if 
                                    // only kick has been specified as the punishement
                                }
                            }
                            catch (Exception ex) { _log.Warn(ex, "I can't apply punishment"); }
                            break;
                        case PunishmentAction.Ban:
                            try
                            {
                                await gu.Guild.AddBanAsync(gu, 7).ConfigureAwait(false);
                            }
                            catch (Exception ex) { _log.Warn(ex, "I can't apply punishment"); }
                            break;
                    }
                }
                await LogCommands.TriggeredAntiProtection(gus, action, pt).ConfigureAwait(false);
            }

            private string GetAntiSpamString(AntiSpamStats stats)
            {
                var ignoredString = string.Join(", ", stats.AntiSpamSettings.IgnoredChannels.Select(c => $"<#{c.ChannelId}>"));

                if (string.IsNullOrWhiteSpace(ignoredString))
                    ignoredString = "none";
                return GetText("spam_stats",
                        Format.Bold(stats.AntiSpamSettings.MessageThreshold.ToString()), 
                        Format.Bold(stats.AntiSpamSettings.Action.ToString()), 
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

                AntiRaidStats throwaway;
                if (_antiRaidGuilds.TryRemove(Context.Guild.Id, out throwaway))
                {
                    using (var uow = DbHandler.UnitOfWork())
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
                    await MuteCommands.GetMuteRole(Context.Guild).ConfigureAwait(false);
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

                _antiRaidGuilds.AddOrUpdate(Context.Guild.Id, stats, (key, old) => stats);

                using (var uow = DbHandler.UnitOfWork())
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
            public async Task AntiSpam(int messageCount = 3, PunishmentAction action = PunishmentAction.Mute)
            {
                if (messageCount < 2 || messageCount > 10)
                    return;

                AntiSpamStats throwaway;
                if (_antiSpamGuilds.TryRemove(Context.Guild.Id, out throwaway))
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting)
                            .ThenInclude(x => x.IgnoredChannels));

                        gc.AntiSpamSetting = null;
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    await ReplyConfirmLocalized("prot_disable", "Anti-Spam").ConfigureAwait(false);
                    return;
                }

                try
                {
                    await MuteCommands.GetMuteRole(Context.Guild).ConfigureAwait(false);
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
                    }
                };

                _antiSpamGuilds.AddOrUpdate(Context.Guild.Id, stats, (key, old) => stats);

                using (var uow = DbHandler.UnitOfWork())
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting));

                    gc.AntiSpamSetting = stats.AntiSpamSettings;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await Context.Channel.SendConfirmAsync(GetText("prot_enable", "Anti-Spam"), $"{Context.User.Mention} {GetAntiSpamString(stats)}").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AntispamIgnore()
            {
                var channel = (ITextChannel)Context.Channel;

                var obj = new AntiSpamIgnore()
                {
                    ChannelId = channel.Id
                };
                bool added;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting).ThenInclude(x => x.IgnoredChannels));
                    var spam = gc.AntiSpamSetting;
                    if (spam == null)
                    {
                        return;
                    }

                    if (spam.IgnoredChannels.Add(obj))
                    {
                        AntiSpamStats temp;
                        if (_antiSpamGuilds.TryGetValue(Context.Guild.Id, out temp))
                            temp.AntiSpamSettings.IgnoredChannels.Add(obj);
                        added = true;
                    }
                    else
                    {
                        spam.IgnoredChannels.Remove(obj);
                        AntiSpamStats temp;
                        if (_antiSpamGuilds.TryGetValue(Context.Guild.Id, out temp))
                            temp.AntiSpamSettings.IgnoredChannels.Remove(obj);
                        added = false;
                    }

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                if (added)
                    await ReplyConfirmLocalized("spam_ignore", "Anti-Spam").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("spam_not_ignore", "Anti-Spam").ConfigureAwait(false);

            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AntiList()
            {
                AntiSpamStats spam;
                _antiSpamGuilds.TryGetValue(Context.Guild.Id, out spam);

                AntiRaidStats raid;
                _antiRaidGuilds.TryGetValue(Context.Guild.Id, out raid);

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