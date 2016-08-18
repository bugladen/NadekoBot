using Discord;
using Discord.Commands;
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
        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task ServerInfo(IMessage msg, string guild = null)
        {
            var channel = msg.Channel as ITextChannel;
            guild = guild?.ToUpperInvariant();
            IGuild server;
            if (guild == null)
                server = channel.Guild;
            else
                server = (await _client.GetGuildsAsync()).Where(g => g.Name.ToUpperInvariant() == guild.ToUpperInvariant()).FirstOrDefault();
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
`Created At:` **{createdAt}**");
            if (server.Emojis.Count() > 0)
                sb.AppendLine($"`Custom Emojis:` **{string.Join(", ", server.Emojis)}**");
            if (server.Features.Count() > 0)
                sb.AppendLine($"`Features:` **{string.Join(", ", server.Features)}**");
            if (!string.IsNullOrWhiteSpace(server.SplashUrl))
                sb.AppendLine($"`Region:` **{server.VoiceRegionId}**");
            await msg.Reply(sb.ToString()).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task ChannelInfo(IMessage msg, ITextChannel channel = null)
        {
            var ch = channel ?? msg.Channel as ITextChannel;
            if (ch == null)
                return;
            var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ch.Id >> 22);
            var sb = new StringBuilder();
            sb.AppendLine($"`Name:` **#{ch.Name}**");
            sb.AppendLine($"`Id:` **{ch.Id}**");
            sb.AppendLine($"`Created At:` **{createdAt}**");
            sb.AppendLine($"`Topic:` **{ch.Topic}**");
            sb.AppendLine($"`Users:` **{(await ch.GetUsersAsync()).Count()}**");
            await msg.Reply(sb.ToString()).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task UserInfo(IMessage msg, IGuildUser usr = null)
        {
            var channel = msg.Channel as ITextChannel;
            var user = usr ?? msg.Author as IGuildUser;
            if (user == null)
                return;
            var sb = new StringBuilder();
            sb.AppendLine($"`Name#Discrim:` **#{user.Username}#{user.Discriminator}**");
            if (!string.IsNullOrWhiteSpace(user.Nickname))
                sb.AppendLine($"`Nickname:` **{user.Nickname}**");
            sb.AppendLine($"`Id:` **{user.Id}**");
            sb.AppendLine($"`Current Game:` **{(user.Game?.Name == null ? "-" : user.Game.Name)}**");
            sb.AppendLine($"`Joined At:` **{user.JoinedAt}**");
            sb.AppendLine($"`Roles:` **({user.Roles.Count()}) - {string.Join(", ", user.Roles.Select(r => r.Name))}**");
            sb.AppendLine($"`AvatarUrl:` **{user.AvatarUrl}**");
            await msg.Reply(sb.ToString()).ConfigureAwait(false);
        }

    }
}