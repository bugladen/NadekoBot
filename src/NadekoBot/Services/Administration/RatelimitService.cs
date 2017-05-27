using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Services.Administration
{
    public class RatelimitService
    {
        public ConcurrentDictionary<ulong, Ratelimiter> RatelimitingChannels = new ConcurrentDictionary<ulong, Ratelimiter>();
        public ConcurrentDictionary<ulong, HashSet<ulong>> IgnoredRoles = new ConcurrentDictionary<ulong, HashSet<ulong>>();
        public ConcurrentDictionary<ulong, HashSet<ulong>> IgnoredUsers = new ConcurrentDictionary<ulong, HashSet<ulong>>();

        private readonly Logger _log;
        private readonly DiscordShardedClient _client;

        public RatelimitService(DiscordShardedClient client, IEnumerable<GuildConfig> gcs)
        {
            _log = LogManager.GetCurrentClassLogger();
            _client = client;

            IgnoredRoles = new ConcurrentDictionary<ulong, HashSet<ulong>>(
                gcs.ToDictionary(x => x.GuildId,
                                 x => new HashSet<ulong>(x.SlowmodeIgnoredRoles.Select(y => y.RoleId))));

            IgnoredUsers = new ConcurrentDictionary<ulong, HashSet<ulong>>(
                gcs.ToDictionary(x => x.GuildId,
                                 x => new HashSet<ulong>(x.SlowmodeIgnoredUsers.Select(y => y.UserId))));

            _client.MessageReceived += async (umsg) =>
            {
                try
                {
                    var usrMsg = umsg as SocketUserMessage;
                    var channel = usrMsg?.Channel as SocketTextChannel;

                    if (channel == null || usrMsg == null || usrMsg.IsAuthor(client))
                        return;
                    if (!RatelimitingChannels.TryGetValue(channel.Id, out Ratelimiter limiter))
                        return;

                    if (limiter.CheckUserRatelimit(usrMsg.Author.Id, channel.Guild.Id, usrMsg.Author as SocketGuildUser))
                        await usrMsg.DeleteAsync();
                }
                catch (Exception ex) { _log.Warn(ex); }
            };
        }
    }
}
