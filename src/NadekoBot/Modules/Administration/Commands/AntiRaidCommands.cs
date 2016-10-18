using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        public enum PunishmentAction
        {
            Mute,
            Kick,
            Ban,
        }

        private class AntiRaidSetting
        {
            public int UserThreshold { get; set; }
            public int Seconds { get; set; }
            public PunishmentAction Action { get; set; }
            public IRole MuteRole { get; set; }
            public int UsersCount { get; set; }
            public ConcurrentHashSet<IGuildUser> RaidUsers { get; set; } = new ConcurrentHashSet<IGuildUser>();
        }

        private class AntiSpamSetting
        {
            public PunishmentAction Action { get; set; }
            public int MessageThreshold { get; set; } = 3;
            public IRole MuteRole { get; set; }
            public ConcurrentDictionary<ulong, UserSpamStats> UserStats { get; set; }
                = new ConcurrentDictionary<ulong, UserSpamStats>();
        }

        private class UserSpamStats
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
        public class AntiRaidCommands
        {
            private static ConcurrentDictionary<ulong, AntiRaidSetting> antiRaidGuilds =
                    new ConcurrentDictionary<ulong, AntiRaidSetting>();
            // guildId | (userId|messages)
            private static ConcurrentDictionary<ulong, AntiSpamSetting> antiSpamGuilds =
                    new ConcurrentDictionary<ulong, AntiSpamSetting>();

            private Logger _log { get; }

            public AntiRaidCommands(ShardedDiscordClient client)
            {
                _log = LogManager.GetCurrentClassLogger();

                client.MessageReceived += (imsg) =>
                {
                    var msg = imsg as IUserMessage;
                    if (msg == null)
                        return Task.CompletedTask;

                    var channel = msg.Channel as ITextChannel;
                    if (channel == null)
                        return Task.CompletedTask;

                    var t = Task.Run(async () =>
                    {
                        AntiSpamSetting spamSettings;
                        if (!antiSpamGuilds.TryGetValue(channel.Guild.Id, out spamSettings))
                            return;

                        var stats = spamSettings.UserStats.AddOrUpdate(msg.Author.Id, new UserSpamStats(msg.Content),
                            (id, old) => { old.ApplyNextMessage(msg.Content); return old; });

                        if (stats.Count >= spamSettings.MessageThreshold)
                        {
                            if (spamSettings.UserStats.TryRemove(msg.Author.Id, out stats))
                            {
                                var log = await PunishUser((IGuildUser)msg.Author, spamSettings.Action, spamSettings.MuteRole)
                                    .ConfigureAwait(false);
                                try { await channel.Guild.SendMessageToOwnerAsync(log).ConfigureAwait(false); } catch { }
                            }
                        }                       

                    });
                    return Task.CompletedTask;
                };

                client.UserJoined += (usr) =>
                {
                    if (usr.IsBot)
                        return Task.CompletedTask;

                    AntiRaidSetting settings;
                    if (!antiRaidGuilds.TryGetValue(usr.Guild.Id, out settings))
                        return Task.CompletedTask;

                    var t = Task.Run(async () =>
                    {
                        if (!settings.RaidUsers.Add(usr))
                            return;

                        ++settings.UsersCount;

                        if (settings.UsersCount >= settings.UserThreshold)
                        {
                            var users = settings.RaidUsers.ToList();
                            settings.RaidUsers.Clear();
                            string msg = "";
                            foreach (var gu in users)
                            {
                                msg += await PunishUser(gu, settings.Action, settings.MuteRole).ConfigureAwait(false);
                            }
                            try { await usr.Guild.SendMessageToOwnerAsync(msg).ConfigureAwait(false); } catch { }
                        }

                        await Task.Delay(1000 * settings.Seconds).ConfigureAwait(false);

                        settings.RaidUsers.TryRemove(usr);
                        --settings.UsersCount;
                    });

                    return Task.CompletedTask;
                };
            }
            
            private async Task<string> PunishUser(IGuildUser gu, PunishmentAction action, IRole muteRole)
            {
                switch (action)
                {
                    case PunishmentAction.Mute:
                        try
                        {
                            await gu.AddRolesAsync(muteRole);
                            return $"{Format.Bold(gu.ToString())} was muted due to raiding protection.\n";
                        }
                        catch (Exception ex) { _log.Warn(ex, "I can't apply punishement"); }
                        break;
                    case PunishmentAction.Kick:
                        try
                        {
                            await gu.Guild.AddBanAsync(gu, 7);
                            try
                            {
                                await gu.Guild.RemoveBanAsync(gu);
                            }
                            catch
                            {
                                await gu.Guild.RemoveBanAsync(gu);
                                // try it twice, really don't want to ban user if 
                                // only kick has been specified as the punishement
                            }
                            return $"{Format.Bold(gu.ToString())} was kicked due to raiding protection.\n";

                        }
                        catch (Exception ex) { _log.Warn(ex, "I can't apply punishment"); }
                        break;
                    case PunishmentAction.Ban:
                        try
                        {
                            await gu.Guild.AddBanAsync(gu, 7);
                            return $"{Format.Bold(gu.ToString())} was banned due to raiding protection.\n";
                        }
                        catch (Exception ex) { _log.Warn(ex, "I can't apply punishment"); }
                        break;
                    default:
                        break;
                }
                return String.Empty;
            }


            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.Administrator)]
            public async Task AntiRaid(IUserMessage imsg, int userThreshold, int seconds, PunishmentAction action)
            {
                var channel = (ITextChannel)imsg.Channel;

                if (userThreshold < 2 || userThreshold > 30)
                {
                    await channel.SendMessageAsync("User threshold must be between 2 and 30").ConfigureAwait(false);
                    return;
                }

                if (seconds < 2 || seconds > 300)
                {
                    await channel.SendMessageAsync("Time must be between 2 and 300 seconds.").ConfigureAwait(false);
                    return;
                }

                IRole muteRole;
                try
                {
                    muteRole = await GetMuteRole(channel.Guild).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await channel.SendMessageAsync("Failed creating a mute role. Give me ManageRoles permission" +
                        "or create 'nadeko-mute' role with disabled SendMessages and try again.")
                            .ConfigureAwait(false);
                    _log.Warn(ex);
                    return;
                }

                var setting = new AntiRaidSetting()
                {
                    Action = action,
                    Seconds = seconds,
                    UserThreshold = userThreshold,
                    MuteRole = muteRole,
                };
                antiRaidGuilds.AddOrUpdate(channel.Guild.Id, setting, (id, old) => setting);

                await channel.SendMessageAsync($"{imsg.Author.Mention} `If {userThreshold} or more users join within {seconds} seconds, I will {action} them.`")
                        .ConfigureAwait(false);
            }

            private async Task<IRole> GetMuteRole(IGuild guild)
            {
                var muteRole = guild.Roles.FirstOrDefault(r => r.Name == "nadeko-mute") ??
                                await guild.CreateRoleAsync("nadeko-mute", GuildPermissions.None).ConfigureAwait(false);
                foreach (var toOverwrite in guild.GetTextChannels())
                {
                    await toOverwrite.AddPermissionOverwriteAsync(muteRole, new OverwritePermissions(sendMessages: PermValue.Deny, attachFiles: PermValue.Deny))
                            .ConfigureAwait(false);
                    await Task.Delay(200).ConfigureAwait(false);
                }
                return muteRole;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.Administrator)]
            public async Task AntiSpam(IUserMessage imsg, PunishmentAction action = PunishmentAction.Mute)
            {
                var channel = (ITextChannel)imsg.Channel;

                AntiSpamSetting throwaway;
                if (antiSpamGuilds.TryRemove(channel.Guild.Id, out throwaway))
                {
                    await channel.SendMessageAsync("`Anti-Spam feature disabled on this server.`").ConfigureAwait(false);
                }
                else
                {
                    if (antiSpamGuilds.TryAdd(channel.Guild.Id, new AntiSpamSetting()
                    {
                        Action = action,
                        MuteRole = await GetMuteRole(channel.Guild).ConfigureAwait(false),
                    }))
                        await channel.SendMessageAsync("`Anti-Spam feature enabled on this server.`").ConfigureAwait(false);
                }

            }
        }
    }
}
