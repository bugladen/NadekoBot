using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Common;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NLog;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace NadekoBot.Modules.Administration.Services
{
    public class SlowmodeService : IEarlyBlocker, INService
    {
        // todo merge into one dictionary
        public ConcurrentDictionary<ulong, Ratelimiter> RatelimitingChannels = new ConcurrentDictionary<ulong, Ratelimiter>();
        public ConcurrentDictionary<ulong, HashSet<ulong>> IgnoredRoles = new ConcurrentDictionary<ulong, HashSet<ulong>>();
        public ConcurrentDictionary<ulong, HashSet<ulong>> IgnoredUsers = new ConcurrentDictionary<ulong, HashSet<ulong>>();
        private Timer _deleteTimer;
        public readonly ConcurrentDictionary<(ulong GuildId, ulong ChannelId), ConcurrentQueue<ulong>> _slowmodeToDelete 
            = new ConcurrentDictionary<(ulong, ulong), ConcurrentQueue<ulong>>();
        private readonly DbService _db;
        private readonly Logger _log;
        private readonly DiscordSocketClient _client;

        public SlowmodeService(DiscordSocketClient client, NadekoBot bot, DbService db)
        {
            _log = LogManager.GetCurrentClassLogger();
            _client = client;
            _db = db;

            IgnoredRoles = new ConcurrentDictionary<ulong, HashSet<ulong>>(
                bot.AllGuildConfigs.ToDictionary(x => x.GuildId,
                                 x => new HashSet<ulong>(x.SlowmodeIgnoredRoles.Select(y => y.RoleId))));

            IgnoredUsers = new ConcurrentDictionary<ulong, HashSet<ulong>>(
                bot.AllGuildConfigs.ToDictionary(x => x.GuildId,
                                 x => new HashSet<ulong>(x.SlowmodeIgnoredUsers.Select(y => y.UserId))));

            _deleteTimer = new Timer(TimerFunc, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
        }

        public async void TimerFunc(object _)
        {
            try
            {
                await Task.WhenAll(_slowmodeToDelete.Keys
                    .ToArray()
                    .Select(async x =>
                    {
                        await Task.Yield();
                        if(_slowmodeToDelete.TryRemove(x, out var q))
                        {
                            var list = new List<ulong>();
                            while(q.TryDequeue(out var id))
                            {
                                list.Add(id);
                            }

                            var g = _client.GetGuild(x.GuildId);
                            var ch = g?.GetChannel(x.ChannelId) as SocketTextChannel;
                            if (ch == null)
                                return;

                            if (!list.Any())
                                return;

                            var manualDelete = list.Take(5);
                            var msgs = await Task.WhenAll(manualDelete.Select(y => ch.GetMessageAsync(y)));
                            var __ = Task.WhenAll(msgs
                                .Where(m => m != null)
                                .Select(m => m.DeleteAsync()));

                            var rem = list.Skip(5).ToArray();
                            if (rem.Any())
                            {
                                await ch.DeleteMessagesAsync(rem);
                            }
                        }
                    }));
            }
            catch { }
        }

        public async Task<bool> TryBlockEarly(IGuild g, IUserMessage usrMsg)
        {
            await Task.Yield();
            try
            {
                var guild = g as SocketGuild;
                var channel = usrMsg?.Channel as SocketTextChannel;
                if (guild == null || channel == null || usrMsg == null || usrMsg.IsAuthor(_client))
                    return false;

                var user = guild.GetUser(usrMsg.Author.Id);
                // ignore users with managemessages permission
                if (guild.OwnerId == user.Id || user.GetPermissions(channel).ManageMessages)
                    return false;
                // see if there is a ratelimiter active
                if (!RatelimitingChannels.TryGetValue(channel.Id, out Ratelimiter limiter))
                    return false;

                if (CheckUserRatelimit(limiter, channel.Guild.Id, usrMsg.Author.Id, usrMsg.Author as SocketGuildUser))
                {
                    // if message is ratelimited, schedule it for deletion
                    _slowmodeToDelete.AddOrUpdate((channel.Guild.Id, channel.Id),
                        new ConcurrentQueue<ulong>(new[] { usrMsg.Id }),
                        (key, old) =>
                        {
                            old.Enqueue(usrMsg.Id);
                            return old;
                        });
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
                
            }
            return false;
        }

        private bool CheckUserRatelimit(Ratelimiter rl, ulong guildId, ulong userId, SocketGuildUser optUser)
        {
            if ((IgnoredUsers.TryGetValue(guildId, out HashSet<ulong> ignoreUsers) && ignoreUsers.Contains(userId)) ||
                   (optUser != null && IgnoredRoles.TryGetValue(guildId, out HashSet<ulong> ignoreRoles) && optUser.Roles.Any(x => ignoreRoles.Contains(x.Id))))
                return false;

            var msgCount = rl.Users.AddOrUpdate(userId, 1, (key, old) => ++old);

            if(msgCount > rl.MaxMessages)
            {
                var test = rl.Users.AddOrUpdate(userId, 0, (key, old) => --old);
                return true;
            }

            var _ = Task.Run(async () =>
            {
                await Task.Delay(rl.PerSeconds * 1000);
                var newVal = rl.Users.AddOrUpdate(userId, 0, (key, old) => --old);
            });
            return false;
        }

        public bool ToggleWhitelistUser(ulong guildId, ulong userId)
        {
            // create db object
            var siu = new SlowmodeIgnoredUser
            {
                UserId = userId
            };

            HashSet<SlowmodeIgnoredUser> usrs;
            bool removed;
            using (var uow = _db.UnitOfWork)
            {
                usrs = uow.GuildConfigs.For(guildId, set => set.Include(x => x.SlowmodeIgnoredUsers))
                    .SlowmodeIgnoredUsers;

                // try removing - if remove is unsuccessful, add
                if (!(removed = usrs.Remove(siu)))
                    usrs.Add(siu);

                uow.Complete();
            }

            // update ignored users in the dictionary
            IgnoredUsers.AddOrUpdate(guildId,
                new HashSet<ulong>(usrs.Select(x => x.UserId)),
                (key, old) => new HashSet<ulong>(usrs.Select(x => x.UserId)));

            return !removed;
        }

        public bool ToggleWhitelistRole(ulong guildId, ulong roleId)
        {
            // create the database object
            var sir = new SlowmodeIgnoredRole
            {
                RoleId = roleId
            };

            HashSet<SlowmodeIgnoredRole> roles;
            bool removed;
            using (var uow = _db.UnitOfWork)
            {
                roles = uow.GuildConfigs.For(guildId, set => set.Include(x => x.SlowmodeIgnoredRoles))
                    .SlowmodeIgnoredRoles;
                // try removing the role - if it's not removed, add it
                if (!(removed = roles.Remove(sir)))
                    roles.Add(sir);

                uow.Complete();
            }
            // completely update the ignored roles in the dictionary
            IgnoredRoles.AddOrUpdate(guildId,
                new HashSet<ulong>(roles.Select(x => x.RoleId)),
                (key, old) => new HashSet<ulong>(roles.Select(x => x.RoleId)));

            return !removed;
        }

        public bool StartSlowmode(ulong id, uint msgCount, int perSec)
        {
            // create a new ratelimiter object which holds the settings
            var rl = new Ratelimiter
            {
                MaxMessages = msgCount,
                PerSeconds = perSec,
            };
            // return whether it's added. If it's not added, the new settings are not applied
            return RatelimitingChannels.TryAdd(id, rl);
        }

        public bool StopSlowmode(ulong id)
        {
            // all we need to do to stop is to remove the settings object from the dictionary
            return RatelimitingChannels.TryRemove(id, out var x);
        }
    }
}
