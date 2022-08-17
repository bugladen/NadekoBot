﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.Collections;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NLog;

namespace NadekoBot.Modules.Administration.Services
{
    public enum MuteType
    {
        Voice,
        Chat,
        All
    }

    public class MuteService : INService
    {
        public ConcurrentDictionary<ulong, string> GuildMuteRoles { get; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> MutedUsers { get; }
        public ConcurrentDictionary<ulong, ConcurrentDictionary<(ulong, TimerType), Timer>> Un_Timers { get; }
            = new ConcurrentDictionary<ulong, ConcurrentDictionary<(ulong, TimerType), Timer>>();

        public event Action<IGuildUser, IUser, MuteType> UserMuted = delegate { };

        public async Task SetMuteRoleAsync(ulong guildId, string name)
        {
            using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigs.ForId(guildId, set => set);
                config.MuteRoleName = name;
                GuildMuteRoles.AddOrUpdate(guildId, name, (id, old) => name);
                await uow.SaveChangesAsync();
            }
        }

        public event Action<IGuildUser, IUser, MuteType> UserUnmuted = delegate { };

        private static readonly OverwritePermissions denyOverwrite = new OverwritePermissions(addReactions: PermValue.Deny, sendMessages: PermValue.Deny, attachFiles: PermValue.Deny);

        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;

        public MuteService(DiscordSocketClient client, NadekoBot bot, DbService db)
        {
            _client = client;
            _db = db;

            GuildMuteRoles = bot
                .AllGuildConfigs
                .Where(c => !string.IsNullOrWhiteSpace(c.MuteRoleName))
                .ToDictionary(c => c.GuildId, c => c.MuteRoleName)
                .ToConcurrent();

            MutedUsers = new ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>>(bot
                .AllGuildConfigs
                .ToDictionary(
                    k => k.GuildId,
                    v => new ConcurrentHashSet<ulong>(v.MutedUsers.Select(m => m.UserId))
            ));

            var max = TimeSpan.FromDays(49);

            foreach (var conf in bot.AllGuildConfigs)
            {
                foreach (var x in conf.UnmuteTimers)
                {
                    TimeSpan after;
                    if (x.UnmuteAt - TimeSpan.FromMinutes(2) <= DateTime.UtcNow)
                    {
                        after = TimeSpan.FromMinutes(2);
                    }
                    else
                    {
                        var unmute = x.UnmuteAt - DateTime.UtcNow;
                        after = unmute > max ?
                            max : unmute;
                    }
                    StartUn_Timer(conf.GuildId, x.UserId, after, TimerType.Mute);
                }

                foreach (var x in conf.UnbanTimer)
                {
                    TimeSpan after;
                    if (x.UnbanAt - TimeSpan.FromMinutes(2) <= DateTime.UtcNow)
                    {
                        after = TimeSpan.FromMinutes(2);
                    }
                    else
                    {
                        var unban = x.UnbanAt - DateTime.UtcNow;
                        after = unban > max ?
                            max : unban;
                    }
                    StartUn_Timer(conf.GuildId, x.UserId, after, TimerType.Ban);
                }
            }

            _client.UserJoined += Client_UserJoined;
        }

        private Task Client_UserJoined(IGuildUser usr)
        {
            try
            {
                MutedUsers.TryGetValue(usr.Guild.Id, out ConcurrentHashSet<ulong> muted);

                if (muted == null || !muted.Contains(usr.Id))
                    return Task.CompletedTask;
                var _ = Task.Run(() => MuteUser(usr, _client.CurrentUser).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
            return Task.CompletedTask;
        }

        public async Task MuteUser(IGuildUser usr, IUser mod, MuteType type = MuteType.All)
        {
            if (type == MuteType.All)
            {
                try { await usr.ModifyAsync(x => x.Mute = true).ConfigureAwait(false); } catch { }
                var muteRole = await GetMuteRole(usr.Guild).ConfigureAwait(false);
                if (!usr.RoleIds.Contains(muteRole.Id))
                    await usr.AddRoleAsync(muteRole).ConfigureAwait(false);
                StopTimer(usr.GuildId, usr.Id, TimerType.Mute);
                using (var uow = _db.GetDbContext())
                {
                    var config = uow.GuildConfigs.ForId(usr.Guild.Id,
                        set => set.Include(gc => gc.MutedUsers)
                            .Include(gc => gc.UnmuteTimers));
                    config.MutedUsers.Add(new MutedUserId()
                    {
                        UserId = usr.Id
                    });
                    if (MutedUsers.TryGetValue(usr.Guild.Id, out ConcurrentHashSet<ulong> muted))
                        muted.Add(usr.Id);

                    config.UnmuteTimers.RemoveWhere(x => x.UserId == usr.Id);

                    await uow.SaveChangesAsync();
                }
                UserMuted(usr, mod, MuteType.All);
            }
            else if (type == MuteType.Voice)
            {
                try
                {
                    await usr.ModifyAsync(x => x.Mute = true).ConfigureAwait(false);
                    UserMuted(usr, mod, MuteType.Voice);
                }
                catch { }
            }
            else if (type == MuteType.Chat)
            {
                await usr.AddRoleAsync(await GetMuteRole(usr.Guild).ConfigureAwait(false)).ConfigureAwait(false);
                UserMuted(usr, mod, MuteType.Chat);
            }
        }

        public async Task UnmuteUser(ulong guildId, ulong usrId, IUser mod, MuteType type = MuteType.All)
        {
            var usr = _client.GetGuild(guildId)?.GetUser(usrId);
            if (type == MuteType.All)
            {
                StopTimer(guildId, usrId, TimerType.Mute);
                using (var uow = _db.GetDbContext())
                {
                    var config = uow.GuildConfigs.ForId(guildId, set => set.Include(gc => gc.MutedUsers)
                        .Include(gc => gc.UnmuteTimers));
                    var match = new MutedUserId()
                    {
                        UserId = usrId
                    };
                    var toRemove = config.MutedUsers.FirstOrDefault(x => x.Equals(match));
                    if (toRemove != null)
                    {
                        uow._context.Remove(toRemove);
                    }
                    if (MutedUsers.TryGetValue(guildId, out ConcurrentHashSet<ulong> muted))
                        muted.TryRemove(usrId);

                    config.UnmuteTimers.RemoveWhere(x => x.UserId == usrId);

                    await uow.SaveChangesAsync();
                }
                if (usr != null)
                {
                    try { await usr.ModifyAsync(x => x.Mute = false).ConfigureAwait(false); } catch { }
                    try { await usr.RemoveRoleAsync(await GetMuteRole(usr.Guild).ConfigureAwait(false)).ConfigureAwait(false); } catch { /*ignore*/ }
                    UserUnmuted(usr, mod, MuteType.All);
                }
            }
            else if (type == MuteType.Voice)
            {
                if (usr == null)
                    return;
                try
                {
                    await usr.ModifyAsync(x => x.Mute = false).ConfigureAwait(false);
                    UserUnmuted(usr, mod, MuteType.Voice);
                }
                catch { }
            }
            else if (type == MuteType.Chat)
            {
                if (usr == null)
                    return;
                await usr.RemoveRoleAsync(await GetMuteRole(usr.Guild).ConfigureAwait(false)).ConfigureAwait(false);
                UserUnmuted(usr, mod, MuteType.Chat);
            }
        }

        public async Task<IRole> GetMuteRole(IGuild guild)
        {
            if (guild == null)
                throw new ArgumentNullException(nameof(guild));

            const string defaultMuteRoleName = "nadeko-mute";

            var muteRoleName = GuildMuteRoles.GetOrAdd(guild.Id, defaultMuteRoleName);

            var muteRole = guild.Roles.FirstOrDefault(r => r.Name == muteRoleName);
            if (muteRole == null)
            {

                //if it doesn't exist, create it 
                try { muteRole = await guild.CreateRoleAsync(muteRoleName, GuildPermissions.None).ConfigureAwait(false); }
                catch
                {
                    //if creations fails,  maybe the name is not correct, find default one, if doesn't work, create default one
                    muteRole = guild.Roles.FirstOrDefault(r => r.Name == muteRoleName) ??
                        await guild.CreateRoleAsync(defaultMuteRoleName, GuildPermissions.None).ConfigureAwait(false);
                }
            }

            foreach (var toOverwrite in (await guild.GetTextChannelsAsync().ConfigureAwait(false)))
            {
                try
                {
                    if (!toOverwrite.PermissionOverwrites.Any(x => x.TargetId == muteRole.Id
                        && x.TargetType == PermissionTarget.Role))
                    {
                        await toOverwrite.AddPermissionOverwriteAsync(muteRole, denyOverwrite)
                                .ConfigureAwait(false);

                        await Task.Delay(200).ConfigureAwait(false);
                    }
                }
                catch
                {
                    // ignored
                }
            }

            return muteRole;
        }

        public async Task TimedMute(IGuildUser user, IUser mod, TimeSpan after)
        {
            await MuteUser(user, mod).ConfigureAwait(false); // mute the user. This will also remove any previous unmute timers
            using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigs.ForId(user.GuildId, set => set.Include(x => x.UnmuteTimers));
                config.UnmuteTimers.Add(new UnmuteTimer()
                {
                    UserId = user.Id,
                    UnmuteAt = DateTime.UtcNow + after,
                }); // add teh unmute timer to the database
                uow.SaveChanges();
            }
            StartUn_Timer(user.GuildId, user.Id, after, TimerType.Mute); // start the timer
        }

        public async Task TimedBan(IGuildUser user, TimeSpan after, string reason)
        {
            await user.Guild.AddBanAsync(user.Id, 0, reason).ConfigureAwait(false);
            using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigs.ForId(user.GuildId, set => set.Include(x => x.UnbanTimer));
                config.UnbanTimer.Add(new UnbanTimer()
                {
                    UserId = user.Id,
                    UnbanAt = DateTime.UtcNow + after,
                }); // add teh unmute timer to the database
                uow.SaveChanges();
            }
            StartUn_Timer(user.GuildId, user.Id, after, TimerType.Ban); // start the timer
        }

        public enum TimerType { Mute, Ban }
        public void StartUn_Timer(ulong guildId, ulong userId, TimeSpan after, TimerType type)
        {
            //load the unmute timers for this guild
            var userUnmuteTimers = Un_Timers.GetOrAdd(guildId, new ConcurrentDictionary<(ulong, TimerType), Timer>());

            //unmute timer to be added
            var toAdd = new Timer(async _ =>
            {
                if (type == TimerType.Ban)
                {
                    try
                    {
                        RemoveTimerFromDb(guildId, userId, type);
                        // unmute the user, this will also remove the timer from the db
                        var guild = _client.GetGuild(guildId); // load the guild
                        if (guild != null)
                        {
                            await guild.RemoveBanAsync(userId).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn("Couldn't unban user {0} in guild {1}", userId, guildId);
                        _log.Warn(ex);
                    }
                }
                else
                {
                    try
                    {
                        // unmute the user, this will also remove the timer from the db
                        await UnmuteUser(guildId, userId, _client.CurrentUser).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        RemoveTimerFromDb(guildId, userId, type); // if unmute errored, just remove unmute from db
                        _log.Warn("Couldn't unmute user {0} in guild {1}", userId, guildId);
                        _log.Warn(ex);
                    }
                }
            }, null, after, Timeout.InfiniteTimeSpan);

            //add it, or stop the old one and add this one
            userUnmuteTimers.AddOrUpdate((userId, type), (key) => toAdd, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return toAdd;
            });
        }

        public void StopTimer(ulong guildId, ulong userId, TimerType type)
        {
            if (!Un_Timers.TryGetValue(guildId, out ConcurrentDictionary<(ulong, TimerType), Timer> userTimer))
                return;

            if (userTimer.TryRemove((userId, type), out Timer removed))
            {
                removed.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private void RemoveTimerFromDb(ulong guildId, ulong userId, TimerType type)
        {
            using (var uow = _db.GetDbContext())
            {
                object toDelete;
                if (type == TimerType.Mute)
                {
                    var config = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.UnmuteTimers));
                    toDelete = config.UnmuteTimers.FirstOrDefault(x => x.UserId == userId);
                }
                else
                {
                    var config = uow.GuildConfigs.ForId(guildId, set => set.Include(x => x.UnbanTimer));
                    toDelete = config.UnbanTimer.FirstOrDefault(x => x.UserId == userId);
                }
                if (toDelete != null)
                {
                    uow._context.Remove(toDelete);
                }
                uow.SaveChanges();
            }
        }
    }
}
