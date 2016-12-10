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
    public partial class Utility
    {
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
                server = NadekoBot.Client.GetGuilds().Where(g => g.Name.ToUpperInvariant() == guild.ToUpperInvariant()).FirstOrDefault();
            if (server == null)
                return;
            var ownername = $"{await server.GetUserAsync(server.OwnerId)}";
            var textchn = $"{(await server.GetTextChannelsAsync()).Count()}";
            var voicechn = $"{(await server.GetVoiceChannelsAsync()).Count()}";
            
            var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(server.Id >> 22);
            var sb = new StringBuilder();
            var users = await server.GetUsersAsync();
            if (server.Emojis.Count() > 0)
            {
                var embed = new EmbedBuilder()
                    .AddField(fb => fb.WithName("**Name**").WithValue($"**{server.Name}**").WithIsInline(true))
                    .AddField(fb => fb.WithName("**ID**").WithValue($"`{server.Id}`").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Owner**").WithValue(ownername).WithIsInline(true))
                    .AddField(fb => fb.WithName("**Members**").WithValue($"**{users.Count}** - {users.Count(u => u.Status == UserStatus.Online)} 💚 {users.Count(u => u.Status == UserStatus.Idle)} 🔶 {users.Count(u => u.Status == UserStatus.DoNotDisturb)} 🔴 {users.Count(u=> u.Status == UserStatus.Offline || u.Status == UserStatus.Unknown)} ⬛️").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Text Channels**").WithValue(textchn).WithIsInline(true))
                    .AddField(fb => fb.WithName("**Voice Channels**").WithValue(voicechn).WithIsInline(true))
                    .AddField(fb => fb.WithName("**Roles**").WithValue($"{server.Roles.Count()}").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Created At**").WithValue($"{createdAt.ToString("dd.MM.yyyy HH:mm")}").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Custom Emojis**").WithValue($"*{string.Join(", ", server.Emojis)}*").WithIsInline(true))
                    .WithThumbnail(tn => tn.Url = $"{ server.IconUrl}")
                    .WithColor(NadekoBot.OkColor);
                await msg.Channel.EmbedAsync(embed.Build()).ConfigureAwait(false);
            }
            else
            {
                var embed = new EmbedBuilder()
                    .AddField(fb => fb.WithName("**Name**").WithValue($"**{server.Name}**").WithIsInline(true))
                    .AddField(fb => fb.WithName("**ID**").WithValue($"`{server.Id}`").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Owner**").WithValue(ownername).WithIsInline(true))
                    .AddField(fb => fb.WithName("**Members**").WithValue($"**{users.Count}** - {users.Count(u => u.Status == UserStatus.Online)} 💚 {users.Count(u => u.Status == UserStatus.Idle)} 🔶 {users.Count(u => u.Status == UserStatus.DoNotDisturb)} 🔴 {users.Count(u=> u.Status == UserStatus.Offline || u.Status == UserStatus.Unknown)} ⬛️").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Text Channels**").WithValue(textchn).WithIsInline(true))
                    .AddField(fb => fb.WithName("**Voice Channels**").WithValue(voicechn).WithIsInline(true))
                    .AddField(fb => fb.WithName("**Roles**").WithValue($"{server.Roles.Count()}").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Created At**").WithValue($"{createdAt.ToString("dd.MM.yyyy HH:mm")}").WithIsInline(true))
                    .WithThumbnail(tn => tn.Url = $"{ server.IconUrl}")
                    .WithColor(NadekoBot.OkColor);
                await msg.Channel.EmbedAsync(embed.Build()).ConfigureAwait(false);
                return;
            }
            //sb.AppendLine($@"__`Name:`__ **{server.Name}**
//__`Owner:`__ **{await server.GetUserAsync(server.OwnerId)}**
//__`ID:`__ **{server.Id}**
//__`Icon URL:`__ { server.IconUrl}
//__`TextChannels:`__ **{(await server.GetTextChannelsAsync()).Count()}** `VoiceChannels:` **{(await server.GetVoiceChannelsAsync()).Count()}**
//__`Members:`__ **{users.Count}** `-` {users.Count(u => u.Status == UserStatus.Online)}💚 {users.Count(u => u.Status == UserStatus.Idle)}🔶 {users.Count(u => u.Status == UserStatus.DoNotDisturb)}🔴 {users.Count(u=> u.Status == UserStatus.Offline || u.Status == UserStatus.Unknown)}⬛️
//__`Roles:`__ **{server.Roles.Count()}**
//__`Created At:`__ **{createdAt.ToString("dd.MM.yyyy HH:mm")}**
//");
            //if (server.Emojis.Count() > 0)
                //sb.AppendLine($"__`Custom Emojis:`__ *{string.Join(", ", server.Emojis)}*");
            //if (server.Features.Count() > 0)
                //sb.AppendLine($"__`Features:`__ **{string.Join(", ", server.Features)}**");
            //if (!string.IsNullOrWhiteSpace(server.SplashUrl))
                //sb.AppendLine($"__`Region:`__ **{server.VoiceRegionId}**");
            //await channel.SendConfirmAsync(sb.ToString()).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChannelInfo(IUserMessage msg, ITextChannel channel = null)
        {
            var ch = channel ?? (ITextChannel)msg.Channel;
            if (ch == null)
                return;
            var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ch.Id >> 22);
            var usercount = (await ch.GetUsersAsync()).Count();
            if (!string.IsNullOrWhiteSpace(ch.Topic))
            {
                var embed = new EmbedBuilder()
                    .WithDescription($"{ch.Topic}")
                    .AddField(fb => fb.WithName("**Name**").WithValue($"#{ch.Name}").WithIsInline(false))
                    .AddField(fb => fb.WithName("**ID**").WithValue($"`{ch.Id}`").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Created At**").WithValue($"{createdAt.ToString("dd.MM.yyyy HH:mm")}").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Users**").WithValue($"{usercount}").WithIsInline(true))
                    .WithColor(NadekoBot.OkColor);
                await msg.Channel.EmbedAsync(embed.Build()).ConfigureAwait(false);
            }
            else
            {
                var embed = new EmbedBuilder()
                    .AddField(fb => fb.WithName("**Name**").WithValue($"#{ch.Name}").WithIsInline(false))
                    .AddField(fb => fb.WithName("**ID**").WithValue($"`{ch.Id}`").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Created At**").WithValue($"{createdAt.ToString("dd.MM.yyyy HH:mm")}").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Users**").WithValue($"{usercount}").WithIsInline(true))
                    .WithColor(NadekoBot.OkColor);
                await msg.Channel.EmbedAsync(embed.Build()).ConfigureAwait(false);
                return;
            }
            //var toReturn = $@"__`Name:`__ **#{ch.Name}**
//__`ID:`__ **{ch.Id}**
//__`Created At:`__ **{createdAt.ToString("dd.MM.yyyy HH:mm")}**
//__`Topic:`__ {ch.Topic}
//__`Users:`__ **{(await ch.GetUsersAsync()).Count()}**";
            //await msg.Channel.SendConfirmAsync(toReturn).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UserInfo(IUserMessage msg, IGuildUser usr = null)
        {
            var channel = (ITextChannel)msg.Channel;
            var user = usr ?? msg.Author as IGuildUser;
            var user1 = msg.Author as IGuildUser;
            //var avurl = await NadekoBot.Google.ShortenUrl(user.AvatarUrl).ConfigureAwait(false);
            if (user == null)
                return;
            if (string.IsNullOrWhiteSpace(user.Nickname))
            {
                var embed = new EmbedBuilder()
                    .AddField(fb => fb.WithName("**Name**").WithValue($"**{user.Username}**#{user.Discriminator}").WithIsInline(false))
                    .AddField(fb => fb.WithName("**ID**").WithValue($"`{user.Id}`").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Joined Server**").WithValue($"{user.JoinedAt?.ToString("dd.MM.yyyy HH:mm")}").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Joined Discord**").WithValue($"{user.CreatedAt.ToString("dd.MM.yyyy HH:mm")}").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Current Game**").WithValue($"{(user.Game?.Name == null ? "-" : user.Game.Name)}").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Roles**").WithValue($"**({user.Roles.Count()})** - {string.Join(", ", user.Roles.Select(r => r.Name)).SanitizeMentions()}").WithIsInline(false))
                    //.AddField(fb => fb.WithName("**Avatar URL**").WithValue(avurl).WithIsInline(true))
                    .WithThumbnail(tn => tn.Url = $"{user.AvatarUrl}")
                    .WithColor(NadekoBot.OkColor);
                await msg.Channel.EmbedAsync(embed.Build()).ConfigureAwait(false);
            }
            else
            {
                var embed = new EmbedBuilder()
                    .AddField(fb => fb.WithName("**Name**").WithValue($"**{user.Username}**#{user.Discriminator}").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Nickname**").WithValue($"{user.Nickname}").WithIsInline(true))
                    .AddField(fb => fb.WithName("**ID**").WithValue($"`{user.Id}`").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Joined Server**").WithValue($"{user.JoinedAt?.ToString("dd.MM.yyyy HH:mm")}").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Joined Discord**").WithValue($"{user.CreatedAt.ToString("dd.MM.yyyy HH:mm")}").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Current Game**").WithValue($"{(user.Game?.Name == null ? "-" : user.Game.Name)}").WithIsInline(true))
                    .AddField(fb => fb.WithName("**Roles**").WithValue($"**({user.Roles.Count()})** - {string.Join(", ", user.Roles.Select(r => r.Name)).SanitizeMentions()}").WithIsInline(false))
                    //.AddField(fb => fb.WithName("**Avatar URL**").WithValue(avurl).WithIsInline(true))
                    .WithThumbnail(tn => tn.Url = $"{user.AvatarUrl}")
                    .WithColor(NadekoBot.OkColor);
                await msg.Channel.EmbedAsync(embed.Build()).ConfigureAwait(false);
                return;
            }
            ////RIP OLD ONE
            //
            //var toReturn = $"👤 __`Name:`__ **{user.Username}#{user.Discriminator}**\n";
            //if (!string.IsNullOrWhiteSpace(user.Nickname))
                //toReturn += $"🆕 __`Nickname:`__ **{user.Nickname}** ";
                //toReturn += $@"🏷 __`ID:`__ **{user.Id}**
//🎮 __`Current Game:`__ **{(user.Game?.Name == null ? "-" : user.Game.Name)}**
//📅 __`Joined Server:`__ **{user.JoinedAt?.ToString("dd.MM.yyyy HH:mm")}** 
//🗓 __`Joined Discord:`__ **{user.CreatedAt.ToString("dd.MM.yyyy HH:mm")}**
//⚔ __`Roles:`__ **({user.Roles.Count()}) - {string.Join(", ", user.Roles.Select(r => r.Name)).SanitizeMentions()}**";
            //if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
                //toReturn += $@"
//📷 __`Avatar URL:`__ **{await NadekoBot.Google.ShortenUrl(user.AvatarUrl).ConfigureAwait(false)}**";
                //await msg.Channel.SendConfirmAsync(toReturn).ConfigureAwait(false);
        }
    }
}
