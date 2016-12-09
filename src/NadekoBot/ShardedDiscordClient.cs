using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;

namespace NadekoBot
{
    public class ShardedDiscordClient 
    {
        private DiscordSocketConfig discordSocketConfig;
        private Logger _log { get; }

        public event Func<IGuildUser, Task> UserJoined = delegate { return Task.CompletedTask; };
        public event Func<IMessage, Task> MessageReceived = delegate { return Task.CompletedTask; };
        public event Func<IGuildUser, Task> UserLeft = delegate { return Task.CompletedTask; };
        public event Func<IGuildUser, IGuildUser, Task> UserUpdated = delegate { return Task.CompletedTask; };
        public event Func<Optional<IMessage>, IMessage, Task> MessageUpdated = delegate { return Task.CompletedTask; };
        public event Func<ulong, Optional<IMessage>, Task> MessageDeleted = delegate { return Task.CompletedTask; };
        public event Func<IUser, IGuild, Task> UserBanned = delegate { return Task.CompletedTask; };
        public event Func<IUser, IGuild, Task> UserUnbanned = delegate { return Task.CompletedTask; };
        public event Func<IGuildUser, IPresence, IPresence, Task> UserPresenceUpdated = delegate { return Task.CompletedTask; };
        public event Func<IUser, IVoiceState, IVoiceState, Task> UserVoiceStateUpdated = delegate { return Task.CompletedTask; };
        public event Func<IChannel, Task> ChannelCreated = delegate { return Task.CompletedTask; };
        public event Func<IChannel, Task> ChannelDestroyed = delegate { return Task.CompletedTask; };
        public event Func<IChannel, IChannel, Task> ChannelUpdated = delegate { return Task.CompletedTask; };
        public event Func<Exception, Task> Disconnected = delegate { return Task.CompletedTask; };

        private IReadOnlyList<DiscordSocketClient> Clients { get; }

        public ShardedDiscordClient (DiscordSocketConfig discordSocketConfig)
        {
            _log = LogManager.GetCurrentClassLogger();
            this.discordSocketConfig = discordSocketConfig;

            var clientList = new List<DiscordSocketClient>();
            for (int i = 0; i < discordSocketConfig.TotalShards; i++)
            {
                discordSocketConfig.ShardId = i;
                var client = new DiscordSocketClient(discordSocketConfig);
                clientList.Add(client);
                client.UserJoined += async arg1 => await UserJoined(arg1);
                client.MessageReceived += async arg1 => await MessageReceived(arg1);
                client.UserLeft += async arg1 => await UserLeft(arg1);
                client.UserUpdated += async (arg1, gu2) => await UserUpdated(arg1, gu2);
                client.MessageUpdated += async (arg1, m2) => await MessageUpdated(arg1, m2);
                client.MessageDeleted += async (arg1, arg2) => await MessageDeleted(arg1, arg2);
                client.UserBanned += async (arg1, arg2) => await UserBanned(arg1, arg2);
                client.UserPresenceUpdated += async (arg1, arg2, arg3) => await UserPresenceUpdated(arg1, arg2, arg3);
                client.UserVoiceStateUpdated += async (arg1, arg2, arg3) => await UserVoiceStateUpdated(arg1, arg2, arg3);
                client.ChannelCreated += async arg => await ChannelCreated(arg);
                client.ChannelDestroyed += async arg => await ChannelDestroyed(arg);
                client.ChannelUpdated += async (arg1, arg2) => await ChannelUpdated(arg1, arg2);

                _log.Info($"Shard #{i} initialized.");
            }

            Clients = clientList.AsReadOnly();
        }

        public ISelfUser GetCurrentUser() =>
            Clients[0].GetCurrentUser();

        public Task<ISelfUser> GetCurrentUserAsync() =>
            Clients[0].GetCurrentUserAsync();

        public Task<ISelfUser[]> GetAllCurrentUsersAsync() =>
            Task.WhenAll(Clients.Select(c => c.GetCurrentUserAsync()));

        public IReadOnlyCollection<IGuild> GetGuilds() =>
            Clients.SelectMany(c => c.GetGuilds()).ToArray();

        public IGuild GetGuild(ulong id) =>
            Clients.Select(c => c.GetGuild(id)).FirstOrDefault(g => g != null);

        public Task<IDMChannel> GetDMChannelAsync(ulong channelId) =>
            Clients[0].GetDMChannelAsync(channelId);

        internal Task LoginAsync(TokenType tokenType, string token) =>
            Task.WhenAll(Clients.Select(async c => { await c.LoginAsync(tokenType, token); _log.Info($"Shard #{c.ShardId} logged in."); }));

        internal Task ConnectAsync() =>
            Task.WhenAll(Clients.Select(async c => { await c.ConnectAsync(); _log.Info($"Shard #{c.ShardId} connected."); }));

        internal Task DownloadAllUsersAsync() =>
            Task.WhenAll(Clients.Select(async c => { await c.DownloadAllUsersAsync(); _log.Info($"Shard #{c.ShardId} downloaded {c.GetGuilds().Sum(g => g.GetUsers().Count)} users."); }));

        public async Task SetGame(string game)
        {
            await Task.WhenAll((await GetAllCurrentUsersAsync())
                                    .Select(u => u.ModifyStatusAsync(ms => ms.Game = new Discord.Game(game))));
        }

        public async Task SetStream(string name, string url)
        {
            await Task.WhenAll((await GetAllCurrentUsersAsync())
                                    .Select(u => u.ModifyStatusAsync(ms => ms.Game = new Discord.Game(name, url, StreamType.Twitch))));
                
        }
    }
}
