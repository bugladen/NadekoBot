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
        public async Task ServerInfo(IUserMessage msg, string guildName = null)
        {
            var channel = (ITextChannel)msg.Channel;
            guildName = guildName?.ToUpperInvariant();
            IGuild guild;
            if (string.IsNullOrWhiteSpace(guildName))
                guild = channel.Guild;
            else
                guild = NadekoBot.Client.GetGuilds().Where(g => g.Name.ToUpperInvariant() == guildName.ToUpperInvariant()).FirstOrDefault();
            if (guild == null)
                return;
            var ownername = await guild.GetUserAsync(guild.OwnerId);
            var textchn = (await guild.GetTextChannelsAsync()).Count();
            var voicechn = (await guild.GetVoiceChannelsAsync()).Count();
            
            var createdAt = new DateTime(2015, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(guild.Id >> 22);
            var sb = new StringBuilder();
            var users = await guild.GetUsersAsync().ConfigureAwait(false);
            var embed = new EmbedBuilder()
                .WithAuthor(eab => eab.WithName("Server Info"))
                .WithTitle(guild.Name)
                .AddField(fb => fb.WithName("**ID**").WithValue(guild.Id.ToString()).WithIsInline(true))
                .AddField(fb => fb.WithName("**Owner**").WithValue(ownername.ToString()).WithIsInline(true))
                .AddField(fb => fb.WithName("**Members**").WithValue(users.Count.ToString()).WithIsInline(true))
                .AddField(fb => fb.WithName("**Text Channels**").WithValue(textchn.ToString()).WithIsInline(true))
                .AddField(fb => fb.WithName("**Voice Channels**").WithValue(voicechn.ToString()).WithIsInline(true))
                .AddField(fb => fb.WithName("**Created At**").WithValue($"{createdAt.ToString("dd.MM.yyyy HH:mm")}").WithIsInline(true))
                .AddField(fb => fb.WithName("**Region**").WithValue(guild.VoiceRegionId.ToString()).WithIsInline(true))
                .AddField(fb => fb.WithName("**Roles**").WithValue(guild.Roles.Count().ToString()).WithIsInline(true))
                .WithImage(tn => tn.WithUrl(guild.IconUrl))
                .WithColor(NadekoBot.OkColor);
            if (guild.Emojis.Count() > 0)
            {
                embed.AddField(fb => fb.WithName("**Custom Emojis**").WithValue(Format.Italics(string.Join(", ", guild.Emojis))).WithIsInline(true));
            }
            await msg.Channel.EmbedAsync(embed.Build()).ConfigureAwait(false);
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
            var embed = new EmbedBuilder()
                .WithTitle(ch.Name)
                .WithDescription(ch.Topic?.SanitizeMentions())
                .AddField(fb => fb.WithName("**ID**").WithValue(ch.Id.ToString()).WithIsInline(true))
                .AddField(fb => fb.WithName("**Created At**").WithValue($"{createdAt.ToString("dd.MM.yyyy HH:mm")}").WithIsInline(true))
                .AddField(fb => fb.WithName("**Users**").WithValue(usercount.ToString()).WithIsInline(true))
                .WithColor(NadekoBot.OkColor);
            await msg.Channel.EmbedAsync(embed.Build()).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UserInfo(IUserMessage msg, IGuildUser usr = null)
        {
            var channel = (ITextChannel)msg.Channel;
            var user = usr ?? msg.Author as IGuildUser;

            if (user == null)
                return;

            var embed = new EmbedBuilder()
                .AddField(fb => fb.WithName("**Name**").WithValue($"**{user.Username}**#{user.Discriminator}").WithIsInline(true));
            if (!string.IsNullOrWhiteSpace(user.Nickname)) {
                embed.AddField(fb => fb.WithName("**Nickname**").WithValue(user.Nickname).WithIsInline(true));
            }
            embed.AddField(fb => fb.WithName("**ID**").WithValue(user.Id.ToString()).WithIsInline(true))
                .AddField(fb => fb.WithName("**Joined Server**").WithValue($"{user.JoinedAt?.ToString("dd.MM.yyyy HH:mm")}").WithIsInline(true))
                .AddField(fb => fb.WithName("**Joined Discord**").WithValue($"{user.CreatedAt.ToString("dd.MM.yyyy HH:mm")}").WithIsInline(true))
                .AddField(fb => fb.WithName("**Current Game**").WithValue($"{(user.Game?.Name == null ? "-" : user.Game.Name)}").WithIsInline(true))
                .AddField(fb => fb.WithName("**Roles**").WithValue($"**({user.Roles.Count()})** - {string.Join(", ", user.Roles.Select(r => r.Name)).SanitizeMentions()}").WithIsInline(true))
                .WithThumbnail(tn => tn.WithUrl(user.AvatarUrl))
                .WithColor(NadekoBot.OkColor);
            await msg.Channel.EmbedAsync(embed.Build()).ConfigureAwait(false);
        }
    }
}
