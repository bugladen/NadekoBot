using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
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
            public int UsersCount { get; set; } = 0;
            public ConcurrentHashSet<IGuildUser> RaidUsers { get; set; } = new ConcurrentHashSet<IGuildUser>();

            public override string ToString() =>
                $"If **{AntiRaidSettings.UserThreshold}** or more users join within **{AntiRaidSettings.Seconds}** seconds," +
                $" I will **{AntiRaidSettings.Action}** them.";
        }

        public class AntiSpamStats
        {
            public AntiSpamSetting AntiSpamSettings { get; set; }
            public ConcurrentDictionary<ulong, UserSpamStats> UserStats { get; set; }
                = new ConcurrentDictionary<ulong, UserSpamStats>();

            public override string ToString()
            {
                var ignoredString = string.Join(", ", AntiSpamSettings.IgnoredChannels.Select(c => $"<#{c.ChannelId}>"));

                if (string.IsNullOrWhiteSpace(ignoredString))
                    ignoredString = "none";
                return $"If a user posts **{AntiSpamSettings.MessageThreshold}** same messages in a row, I will **{AntiSpamSettings.Action}** them."
                + $"\n\t__IgnoredChannels__: {ignoredString}";
            }
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

            public void ApplyNextMessage(string message)
            {
                var upperMsg = message.ToUpperInvariant();
                if (upperMsg == LastMessage)
                    Count++;
                else
                {
                    LastMessage = upperMsg;
                    Count = 0;
                }
            }
        }

        [Group]
        public class ProtectionCommands : ModuleBase
        {
            private static ConcurrentDictionary<ulong, AntiRaidStats> antiRaidGuilds =
                    new ConcurrentDictionary<ulong, AntiRaidStats>();
            // guildId | (userId|messages)
            private static ConcurrentDictionary<ulong, AntiSpamStats> antiSpamGuilds =
                    new ConcurrentDictionary<ulong, AntiSpamStats>();

            private static Logger _log { get; }

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
                        antiRaidGuilds.TryAdd(gc.GuildId, raidStats);
                    }

                    if (spam != null)
                        antiSpamGuilds.TryAdd(gc.GuildId, new AntiSpamStats() { AntiSpamSettings = spam });
                }

                NadekoBot.Client.MessageReceived += async (imsg) =>
                {

                    try
                    {
                        var msg = imsg as IUserMessage;
                        if (msg == null || msg.Author.IsBot)
                            return;

                        var channel = msg.Channel as ITextChannel;
                        if (channel == null)
                            return;
                        AntiSpamStats spamSettings;
                        if (!antiSpamGuilds.TryGetValue(channel.Guild.Id, out spamSettings) ||
                            spamSettings.AntiSpamSettings.IgnoredChannels.Contains(new AntiSpamIgnore()
                            {
                                ChannelId = channel.Id
                            }))
                            return;

                        var stats = spamSettings.UserStats.AddOrUpdate(msg.Author.Id, new UserSpamStats(msg.Content),
                            (id, old) => { old.ApplyNextMessage(msg.Content); return old; });

                        if (stats.Count >= spamSettings.AntiSpamSettings.MessageThreshold)
                        {
                            if (spamSettings.UserStats.TryRemove(msg.Author.Id, out stats))
                            {
                                await PunishUsers(spamSettings.AntiSpamSettings.Action, ProtectionType.Spamming, (IGuildUser)msg.Author)
                                    .ConfigureAwait(false);
                            }
                        }
                    }
                    catch { }
                };

                NadekoBot.Client.UserJoined += async (usr) =>
                {
                    try
                    {
                        if (usr.IsBot)
                            return;
                        AntiRaidStats settings;
                        if (!antiRaidGuilds.TryGetValue(usr.Guild.Id, out settings))
                            return;
                        if (!settings.RaidUsers.Add(usr))
                            return;

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
                    catch { }
                };
            }

            private static async Task PunishUsers(PunishmentAction action, ProtectionType pt, params IGuildUser[] gus)
            {
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
                        default:
                            break;
                    }
                }
                await LogCommands.TriggeredAntiProtection(gus, action, pt).ConfigureAwait(false);
            }


            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task AntiRaid(int userThreshold = 5, int seconds = 10, PunishmentAction action = PunishmentAction.Mute)
            {
                if (userThreshold < 2 || userThreshold > 30)
                {
                    await Context.Channel.SendErrorAsync("❗️User threshold must be between **2** and **30**.").ConfigureAwait(false);
                    return;
                }

                if (seconds < 2 || seconds > 300)
                {
                    await Context.Channel.SendErrorAsync("❗️Time must be between **2** and **300** seconds.").ConfigureAwait(false);
                    return;
                }

                AntiRaidStats throwaway;
                if (antiRaidGuilds.TryRemove(Context.Guild.Id, out throwaway))
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiRaidSetting));

                        gc.AntiRaidSetting = null;
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    await Context.Channel.SendConfirmAsync("**Anti-Raid** feature has been **disabled** on this server.").ConfigureAwait(false);
                    return;
                }

                try
                {
                    await MuteCommands.GetMuteRole(Context.Guild).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendConfirmAsync("⚠️ Failed creating a mute role. Give me ManageRoles permission" +
                        "or create 'nadeko-mute' role with disabled SendMessages and try again.")
                            .ConfigureAwait(false);
                    _log.Warn(ex);
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

                antiRaidGuilds.AddOrUpdate(Context.Guild.Id, stats, (key, old) => stats);

                using (var uow = DbHandler.UnitOfWork())
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiRaidSetting));

                    gc.AntiRaidSetting = stats.AntiRaidSettings;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await Context.Channel.SendConfirmAsync("Anti-Raid Enabled", $"{Context.User.Mention} {stats.ToString()}")
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
                if (antiSpamGuilds.TryRemove(Context.Guild.Id, out throwaway))
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting)
                            .ThenInclude(x => x.IgnoredChannels));

                        gc.AntiSpamSetting = null;
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    await Context.Channel.SendConfirmAsync("**Anti-Spam** has been **disabled** on this server.").ConfigureAwait(false);
                    return;
                }

                try
                {
                    await MuteCommands.GetMuteRole(Context.Guild).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendErrorAsync("⚠️ Failed creating a mute role. Give me ManageRoles permission" +
                        "or create 'nadeko-mute' role with disabled SendMessages and try again.")
                            .ConfigureAwait(false);
                    _log.Warn(ex);
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

                antiSpamGuilds.AddOrUpdate(Context.Guild.Id, stats, (key, old) => stats);

                using (var uow = DbHandler.UnitOfWork())
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting));

                    gc.AntiSpamSetting = stats.AntiSpamSettings;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await Context.Channel.SendConfirmAsync("Anti-Spam Enabled", $"{Context.User.Mention} {stats.ToString()}").ConfigureAwait(false);
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
                        if (antiSpamGuilds.TryGetValue(Context.Guild.Id, out temp))
                            temp.AntiSpamSettings.IgnoredChannels.Add(obj);
                        added = true;
                    }
                    else
                    {
                        spam.IgnoredChannels.Remove(obj);
                        AntiSpamStats temp;
                        if (antiSpamGuilds.TryGetValue(Context.Guild.Id, out temp))
                            temp.AntiSpamSettings.IgnoredChannels.Remove(obj);
                        added = false;
                    }

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                if (added)
                    await Context.Channel.SendConfirmAsync("Anti-Spam will ignore this channel.").ConfigureAwait(false);
                else
                    await Context.Channel.SendConfirmAsync("Anti-Spam will no longer ignore this channel.").ConfigureAwait(false);

            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AntiList()
            {
                var channel = (ITextChannel)Context.Channel;

                AntiSpamStats spam;
                antiSpamGuilds.TryGetValue(Context.Guild.Id, out spam);

                AntiRaidStats raid;
                antiRaidGuilds.TryGetValue(Context.Guild.Id, out raid);

                if (spam == null && raid == null)
                {
                    await Context.Channel.SendConfirmAsync("No protections enabled.");
                    return;
                }

                var embed = new EmbedBuilder().WithOkColor()
                    .WithTitle("Protections Enabled");

                if (spam != null)
                    embed.AddField(efb => efb.WithName("Anti-Spam")
                        .WithValue(spam.ToString())
                        .WithIsInline(true));

                if (raid != null)
                    embed.AddField(efb => efb.WithName("Anti-Raid")
                        .WithValue(raid.ToString())
                        .WithIsInline(true));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
        }
    }
}