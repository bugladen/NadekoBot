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

            await channel.SendMessageAsync($"🎞 {imsg.Author.Mention}, **Your new video room created. Join and invite to watch videos together with friends:** {target}")
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
            sb.AppendLine($@"__`Name:`__ **{server.Name}**
__`Owner:`__ **{await server.GetUserAsync(server.OwnerId)}**
__`ID:`__ **{server.Id}**
__`Icon URL:`__ { server.IconUrl}
__`TextChannels:`__ **{(await server.GetTextChannelsAsync()).Count()}** `VoiceChannels:` **{(await server.GetVoiceChannelsAsync()).Count()}**
__`Members:`__ **{users.Count}** `-` {users.Count(u => u.Status == UserStatus.Online)}💚 {users.Count(u => u.Status == UserStatus.Idle)}🔶 {users.Count(u => u.Status == UserStatus.DoNotDisturb)}🔴 {users.Count(u=> u.Status == UserStatus.Offline || u.Status == UserStatus.Unknown)}⬛️
__`Roles:`__ **{server.Roles.Count()}**
__`Created At:`__ **{createdAt.ToString("dd.MM.yyyy HH:mm")}**
");
            if (server.Emojis.Count() > 0)
                sb.AppendLine($"__`Custom Emojis:`__ *{string.Join(", ", server.Emojis)}*");
            if (server.Features.Count() > 0)
                sb.AppendLine($"__`Features:`__ **{string.Join(", ", server.Features)}**");
            if (!string.IsNullOrWhiteSpace(server.SplashUrl))
                sb.AppendLine($"__`Region:`__ **{server.VoiceRegionId}**");
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
            var toReturn = $@"__`Name:`__ **#{ch.Name}**
__`ID:`__ **{ch.Id}**
__`Created At:`__ **{createdAt.ToString("dd.MM.yyyy HH:mm")}**
__`Topic:`__ {ch.Topic}
__`Users:`__ **{(await ch.GetUsersAsync()).Count()}**";
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
            var toReturn = $"👤 __`Name:`__ **{user.Username}#{user.Discriminator}**\n";
            if (!string.IsNullOrWhiteSpace(user.Nickname))
                toReturn += $"🆕 __`Nickname:`__ **{user.Nickname}** ";
                toReturn += $@"🏷 __`ID:`__ **{user.Id}**
🎮 __`Current Game:`__ **{(user.Game?.Name == null ? "-" : user.Game.Name)}**
📅 __`Joined Server:`__ **{user.JoinedAt?.ToString("dd.MM.yyyy HH:mm")}** 
🗓 __`Joined Discord:`__ **{user.CreatedAt.ToString("dd.MM.yyyy HH:mm")}**
⚔ __`Roles:`__ **({user.Roles.Count()}) - {string.Join(", ", user.Roles.Select(r => r.Name)).SanitizeMentions()}**";
            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
                toReturn += $@"
📷 __`Avatar URL:`__ **{await NadekoBot.Google.ShortenUrl(user.AvatarUrl).ConfigureAwait(false)}**";
                await msg.Reply(toReturn).ConfigureAwait(false);
        }
    }
}
