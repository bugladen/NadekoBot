using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    partial class Utility : DiscordModule
    {
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task TogetherTube(IUserMessage imsg)
        {
            var channel = (ITextChannel)imsg.Channel;

            Uri target;
            using (var http = new HttpClient())
            {
                var res = await http.GetAsync("https://togethertube.com/room/create").ConfigureAwait(false);
                target = res.RequestMessage.RequestUri;
            }

            await channel.SendMessageAsync($"{imsg.Author.Mention}, `Here is the link:` {target}")
                         .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
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
`Members:` **{users.Count}** `-` {users.Count(u => u.Status == UserStatus.Online)}:green_heart: {users.Count(u => u.Status == UserStatus.Idle)}:yellow_heart: {users.Count(u => u.Status == UserStatus.DoNotDisturb)}:heart: {users.Count(u=> u.Status == UserStatus.Offline || u.Status == UserStatus.Unknown)}:black_heart:
`Roles:` **{server.Roles.Count()}**
`Created At:` **{createdAt.ToString("dd.MM.yyyy HH:mm")}**
");
            if (server.Emojis.Count() > 0)
                sb.AppendLine($"`Custom Emojis:` **{string.Join(", ", server.Emojis)}**");
            if (server.Features.Count() > 0)
                sb.AppendLine($"`Features:` **{string.Join(", ", server.Features)}**");
            if (!string.IsNullOrWhiteSpace(server.SplashUrl))
                sb.AppendLine($"`Region:` **{server.VoiceRegionId}**");
            await msg.Reply(sb.ToString()).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChannelInfo(IUserMessage msg, ITextChannel channel = null)
        {
            var ch = channel ?? (ITextChannel)msg.Channel;
            if (ch == null)
                return;
            var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ch.Id >> 22);
            var toReturn = $@"`Name:` **#{ch.Name}**
`Id:` **{ch.Id}**
`Created At:` **{createdAt.ToString("dd.MM.yyyy HH:mm")}**
`Topic:` **{ch.Topic}**
`Users:` **{(await ch.GetUsersAsync()).Count()}**";
            await msg.Reply(toReturn).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
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
`Joined Server:` **{user.JoinedAt?.ToString("dd.MM.yyyy HH:mm")}** 
`Joined Discord:` **{user.CreatedAt.ToString("dd.MM.yyyy HH:mm")}**
`Roles:` **({user.Roles.Count()}) - {string.Join(", ", user.Roles.Select(r => r.Name)).SanitizeMentions()}**
`AvatarUrl:` **{await NadekoBot.Google.ShortenUrl(user.AvatarUrl).ConfigureAwait(false)}**";
            await msg.Reply(toReturn).ConfigureAwait(false);
        }
    }
}