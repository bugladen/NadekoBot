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

namespace NadekoBot.Modules.Administration.Services
{
    public class SlowmodeService : IEarlyBlocker, INService
    {
        public ConcurrentDictionary<ulong, Ratelimiter> RatelimitingChannels = new ConcurrentDictionary<ulong, Ratelimiter>();
        public ConcurrentDictionary<ulong, HashSet<ulong>> IgnoredRoles = new ConcurrentDictionary<ulong, HashSet<ulong>>();
        public ConcurrentDictionary<ulong, HashSet<ulong>> IgnoredUsers = new ConcurrentDictionary<ulong, HashSet<ulong>>();
        private readonly DbService _db;
        private readonly Logger _log;
        private readonly DiscordSocketClient _client;

        public SlowmodeService(DiscordSocketClient client, NadekoBot bot, DbService db)
        {
            _log = LogManager.GetCurrentClassLogger();
            _client = client;

            IgnoredRoles = new ConcurrentDictionary<ulong, HashSet<ulong>>(
                bot.AllGuildConfigs.ToDictionary(x => x.GuildId,
                                 x => new HashSet<ulong>(x.SlowmodeIgnoredRoles.Select(y => y.RoleId))));

            IgnoredUsers = new ConcurrentDictionary<ulong, HashSet<ulong>>(
                bot.AllGuildConfigs.ToDictionary(x => x.GuildId,
                                 x => new HashSet<ulong>(x.SlowmodeIgnoredUsers.Select(y => y.UserId))));

            _db = db;
        }

        public bool StopSlowmode(ulong id)
        {
           return RatelimitingChannels.TryRemove(id, out var x);
        }

        public async Task<bool> TryBlockEarly(IGuild guild, IUserMessage usrMsg)
        {
            if (guild == null)
                return false;
            try
            {
                var channel = usrMsg?.Channel as SocketTextChannel;

                if (channel == null || usrMsg == null || usrMsg.IsAuthor(_client))
                    return false;
                if (!RatelimitingChannels.TryGetValue(channel.Id, out Ratelimiter limiter))
                    return false;

                if (limiter.CheckUserRatelimit(usrMsg.Author.Id, channel.Guild.Id, usrMsg.Author as SocketGuildUser))
                {
                    await usrMsg.DeleteAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
                
            }
            return false;
        }

        public bool ToggleWhitelistUser(ulong guildId, ulong userId)
        {
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

                if (!(removed = usrs.Remove(siu)))
                    usrs.Add(siu);

                uow.Complete();
            }

            IgnoredUsers.AddOrUpdate(guildId,
                new HashSet<ulong>(usrs.Select(x => x.UserId)),
                (key, old) => new HashSet<ulong>(usrs.Select(x => x.UserId)));

            return !removed;
        }

        public bool ToggleWhitelistRole(ulong guildId, ulong roleId)
        {
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

                if (!(removed = roles.Remove(sir)))
                    roles.Add(sir);

                uow.Complete();
            }

            IgnoredRoles.AddOrUpdate(guildId,
                new HashSet<ulong>(roles.Select(x => x.RoleId)),
                (key, old) => new HashSet<ulong>(roles.Select(x => x.RoleId)));

            return !removed;
        }

        public bool StartSlowmode(ulong id, int msgCount, int perSec)
        {
            var rl = new Ratelimiter(this)
            {
                MaxMessages = msgCount,
                PerSeconds = perSec,
            };

            return RatelimitingChannels.TryAdd(id, rl);
        }

        public bool HasSlowMode(ulong guildId)
        {
            return RatelimitingChannels.ContainsKey(guildId);
        }
    }
}
