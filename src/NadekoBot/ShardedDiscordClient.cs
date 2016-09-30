using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.API;
using Discord.Logging;
using System.IO;

namespace NadekoBot
{
    public class ShardedDiscordClient 
    {
        private DiscordSocketConfig discordSocketConfig;

        public Func<IGuildUser, Task> UserJoined { get; internal set; } = delegate { return Task.CompletedTask; };
        public Func<IMessage, Task> MessageReceived { get; internal set; } = delegate { return Task.CompletedTask; };
        public Func<IGuildUser, Task> UserLeft { get; internal set; } = delegate { return Task.CompletedTask; };
        public Func<IGuildUser, IGuildUser, Task> UserUpdated { get; internal set; } = delegate { return Task.CompletedTask; };
        public Func<Optional<IMessage>, IMessage, Task> MessageUpdated { get; internal set; } = delegate { return Task.CompletedTask; };
        public Func<ulong, Optional<IMessage>, Task> MessageDeleted { get; internal set; } = delegate { return Task.CompletedTask; };
        public Func<IUser, IGuild, Task> UserBanned { get; internal set; } = delegate { return Task.CompletedTask; };
        public Func<IUser, IGuild, Task> UserUnbanned { get; internal set; } = delegate { return Task.CompletedTask; };
        public Func<IGuildUser, IPresence, IPresence, Task> UserPresenceUpdated { get; internal set; } = delegate { return Task.CompletedTask; };
        public Func<IUser, IVoiceState, IVoiceState, Task> UserVoiceStateUpdated { get; internal set; } = delegate { return Task.CompletedTask; };
        public Func<IChannel, Task> ChannelCreated { get; internal set; } = delegate { return Task.CompletedTask; };
        public Func<IChannel, Task> ChannelDestroyed { get; internal set; } = delegate { return Task.CompletedTask; };
        public Func<IChannel, IChannel, Task> ChannelUpdated { get; internal set; } = delegate { return Task.CompletedTask; };
        public Func<Exception, Task> Disconnected { get; internal set; } = delegate { return Task.CompletedTask; };

        private IReadOnlyList<DiscordSocketClient> Clients { get; }

        public ShardedDiscordClient (DiscordSocketConfig discordSocketConfig)
        {
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
            }

            Clients = clientList.AsReadOnly();
        }

        public ISelfUser GetCurrentUser() => 
            Clients.Select(c => c.GetCurrentUser()).FirstOrDefault(u => u != null);

        public IReadOnlyCollection<IGuild> GetGuilds() =>
            Clients.SelectMany(c => c.GetGuilds()).ToArray();

        public IGuild GetGuild(ulong id) =>
            Clients.Select(c => c.GetGuild(id)).FirstOrDefault(g => g != null);

        public Task<IDMChannel> GetDMChannelAsync(ulong channelId) =>
            Clients.Select(async c => await c.GetDMChannelAsync(channelId).ConfigureAwait(false)).FirstOrDefault(c => c != null);

        internal Task LoginAsync(TokenType tokenType, string token) =>
            Task.WhenAll(Clients.Select(c => c.LoginAsync(tokenType, token)));

        internal Task ConnectAsync() =>
            Task.WhenAll(Clients.Select(c => c.ConnectAsync()));

        internal Task DownloadAllUsersAsync() =>
            Task.WhenAll(Clients.Select(c => c.DownloadAllUsersAsync()));
    }
}
