using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, Timer>> UnmuteTimers { get; }
            = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, Timer>>();

        public event Action<IGuildUser, MuteType> UserMuted = delegate { };

        public async Task SetMuteRoleAsync(ulong guildId, string name)
        {
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.GuildConfigs.For(guildId, set => set);
                config.MuteRoleName = name;
                GuildMuteRoles.AddOrUpdate(guildId, name, (id, old) => name);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        public event Action<IGuildUser, MuteType> UserUnmuted = delegate { };

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
                        after = x.UnmuteAt - DateTime.UtcNow;
                    }
                    StartUnmuteTimer(conf.GuildId, x.UserId, after);
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
                var _ = Task.Run(() => MuteUser(usr).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
            return Task.CompletedTask;
        }

        public async Task MuteUser(IGuildUser usr, MuteType type = MuteType.All)
        {
            if (type == MuteType.All)
            {
                await usr.ModifyAsync(x => x.Mute = true).ConfigureAwait(false);
                var muteRole = await GetMuteRole(usr.Guild);
                if (!usr.RoleIds.Contains(muteRole.Id))
                    await usr.AddRoleAsync(muteRole).ConfigureAwait(false);
                StopUnmuteTimer(usr.GuildId, usr.Id);
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.For(usr.Guild.Id,
                        set => set.Include(gc => gc.MutedUsers)
                            .Include(gc => gc.UnmuteTimers));
                    config.MutedUsers.Add(new MutedUserId()
                    {
                        UserId = usr.Id
                    });
                    if (MutedUsers.TryGetValue(usr.Guild.Id, out ConcurrentHashSet<ulong> muted))
                        muted.Add(usr.Id);

                    config.UnmuteTimers.RemoveWhere(x => x.UserId == usr.Id);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                UserMuted(usr, MuteType.All);
            }
            else if (type == MuteType.Voice)
            {
                await usr.ModifyAsync(x => x.Mute = true).ConfigureAwait(false);
                UserMuted(usr, MuteType.Voice);
            }
            else if (type == MuteType.Chat)
            {
                await usr.AddRoleAsync(await GetMuteRole(usr.Guild).ConfigureAwait(false)).ConfigureAwait(false);
                UserMuted(usr, MuteType.Chat);
            }
        }

        public async Task UnmuteUser(IGuildUser usr, MuteType type = MuteType.All)
        {
            if (type == MuteType.All)
            {
                StopUnmuteTimer(usr.GuildId, usr.Id);
                try { await usr.ModifyAsync(x => x.Mute = false).ConfigureAwait(false); } catch { }
                try { await usr.RemoveRoleAsync(await GetMuteRole(usr.Guild)).ConfigureAwait(false); } catch { /*ignore*/ }
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.For(usr.Guild.Id, set => set.Include(gc => gc.MutedUsers)
                        .Include(gc => gc.UnmuteTimers));
                    config.MutedUsers.Remove(new MutedUserId()
                    {
                        UserId = usr.Id
                    });
                    if (MutedUsers.TryGetValue(usr.Guild.Id, out ConcurrentHashSet<ulong> muted))
                        muted.TryRemove(usr.Id);

                    config.UnmuteTimers.RemoveWhere(x => x.UserId == usr.Id);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                UserUnmuted(usr, MuteType.All);
            }
            else if (type == MuteType.Voice)
            {
                await usr.ModifyAsync(x => x.Mute = false).ConfigureAwait(false);
                UserUnmuted(usr, MuteType.Voice);
            }
            else if (type == MuteType.Chat)
            {
                await usr.RemoveRoleAsync(await GetMuteRole(usr.Guild).ConfigureAwait(false)).ConfigureAwait(false);
                UserUnmuted(usr, MuteType.Chat);
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

            foreach (var toOverwrite in (await guild.GetTextChannelsAsync()))
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

        public async Task TimedMute(IGuildUser user, TimeSpan after)
        {
            await MuteUser(user).ConfigureAwait(false); // mute the user. This will also remove any previous unmute timers
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.GuildConfigs.For(user.GuildId, set => set.Include(x => x.UnmuteTimers));
                config.UnmuteTimers.Add(new UnmuteTimer()
                {
                    UserId = user.Id,
                    UnmuteAt = DateTime.UtcNow + after,
                }); // add teh unmute timer to the database
                uow.Complete();
            }
            StartUnmuteTimer(user.GuildId, user.Id, after); // start the timer
        }

        public void StartUnmuteTimer(ulong guildId, ulong userId, TimeSpan after)
        {
            //load the unmute timers for this guild
            var userUnmuteTimers = UnmuteTimers.GetOrAdd(guildId, new ConcurrentDictionary<ulong, Timer>());

            //unmute timer to be added
            var toAdd = new Timer(async _ =>
            {
                try
                {
                    var guild = _client.GetGuild(guildId); // load the guild
                    if (guild == null)
                    {
                        RemoveUnmuteTimerFromDb(guildId, userId);
                        return; // if guild can't be found, just remove the timer from db
                    }
                    // unmute the user, this will also remove the timer from the db
                    await UnmuteUser(guild.GetUser(userId)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    RemoveUnmuteTimerFromDb(guildId, userId); // if unmute errored, just remove unmute from db
                    _log.Warn("Couldn't unmute user {0} in guild {1}", userId, guildId);
                    _log.Warn(ex);
                }
            }, null, after, Timeout.InfiniteTimeSpan);

            //add it, or stop the old one and add this one
            userUnmuteTimers.AddOrUpdate(userId, (key) => toAdd, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return toAdd;
            });
        }

        public void StopUnmuteTimer(ulong guildId, ulong userId)
        {
            if (!UnmuteTimers.TryGetValue(guildId, out ConcurrentDictionary<ulong, Timer> userUnmuteTimers)) return;

            if (userUnmuteTimers.TryRemove(userId, out Timer removed))
            {
                removed.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private void RemoveUnmuteTimerFromDb(ulong guildId, ulong userId)
        {
            using (var uow = _db.UnitOfWork)
            {
                var config = uow.GuildConfigs.For(guildId, set => set.Include(x => x.UnmuteTimers));
                config.UnmuteTimers.RemoveWhere(x => x.UserId == userId);
                uow.Complete();
            }
        }
    }
}
