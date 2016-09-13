using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    partial class Utility : DiscordModule
    {
        [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task ServerInfo(IUserMessage msg, string guild = null)
        {
            var channel = (ITextChannel)msg.Channel;
            guild = guild?.ToUpperInvariant();
            IGuild server;
            if (guild == null)
                server = channel.Guild;
            else
                server = _client.GetGuilds().Where(g => g.Name.ToUpperInvariant() == guild.ToUpperInvariant()).FirstOrDefault();
            if (server == null)
                return;

            var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(server.Id >> 22);
            var sb = new StringBuilder();
            var users = await server.GetUsersAsync();
            sb.AppendLine($@"`Name:` **{server.Name}**
`Owner:` **{await server.GetUserAsync(server.OwnerId)}**
`Id:` **{server.Id}**
`Icon Url:` **{ server.IconUrl}**
`TextChannels:` **{(await server.GetTextChannelsAsync()).Count()}** `VoiceChannels:` **{(await server.GetVoiceChannelsAsync()).Count()}**
`Members:` **{users.Count}** `Online:` **{users.Count(u => u.Status == UserStatus.Online)}**
`Roles:` **{server.Roles.Count()}**
`Created At:` **{createdAt}**
");
            if (server.Emojis.Count() > 0)
                sb.AppendLine($"`Custom Emojis:` **{string.Join(", ", server.Emojis)}**");
            if (server.Features.Count() > 0)
                sb.AppendLine($"`Features:` **{string.Join(", ", server.Features)}**");
            if (!string.IsNullOrWhiteSpace(server.SplashUrl))
                sb.AppendLine($"`Region:` **{server.VoiceRegionId}**");
            await msg.Reply(sb.ToString()).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task ChannelInfo(IUserMessage msg, ITextChannel channel = null)
        {
            var ch = channel ?? (ITextChannel)msg.Channel;
            if (ch == null)
                return;
            var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ch.Id >> 22);
            var toReturn = $@"`Name:` **#{ch.Name}**
`Id:` **{ch.Id}**
`Created At:` **{createdAt}**
`Topic:` **{ch.Topic}**
`Users:` **{(await ch.GetUsersAsync()).Count()}**";
            await msg.Reply(toReturn).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task UserInfo(IUserMessage msg, IGuildUser usr = null)
        {
            var channel = (ITextChannel)msg.Channel;
            var user = usr ?? msg.Author as IGuildUser;
            if (user == null)
                return;
            var toReturn = $"`Name#Discrim:` **#{user.Username}#{user.Discriminator}**\n";
            if (!string.IsNullOrWhiteSpace(user.Nickname))
                toReturn += $"`Nickname:` **{user.Nickname}**";
            toReturn += $@"`Id:` **{user.Id}**
`Current Game:` **{(user.Game?.Name == null ? "-" : user.Game.Name)}**
`Joined At:` **{user.JoinedAt}**
`Roles:` **({user.Roles.Count()}) - {string.Join(", ", user.Roles.Select(r => r.Name))}**
`AvatarUrl:` **{user.AvatarUrl}**";
            await msg.Reply(toReturn).ConfigureAwait(false);
        }
    }
}