using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Services;
using System.Text;
using NadekoBot.Extensions;
using System.Text.RegularExpressions;
using System.Reflection;
using Discord.WebSocket;
using NadekoBot.Services.Impl;
using Discord.API;
using Embed = Discord.API.Embed;
using EmbedAuthor = Discord.API.EmbedAuthor;
using EmbedField = Discord.API.EmbedField;
using System.Net.Http;

namespace NadekoBot.Modules.Utility
{

    [NadekoModule("Utility", ".")]
    public partial class Utility : DiscordModule
    {
        public Utility() : base()
        {

        }

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

            await channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithAuthor(eab => eab.WithIconUrl("https://togethertube.com/assets/img/favicons/favicon-32x32.png")
                .WithName("Together Tube")
                .WithUrl("https://togethertube.com/"))
                .WithDescription($"{imsg.Author.Mention} Here is your room link:\n{target}")
                .Build());
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task WhosPlaying(IUserMessage umsg, [Remainder] string game = null)
        {
            var channel = (ITextChannel)umsg.Channel;
            game = game.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(game))
                return;
            var usrs = (await (umsg.Channel as IGuildChannel).Guild.GetUsersAsync())
                    .Where(u => u.Game?.Name?.ToUpperInvariant() == game)
                    .Select(u => u.Username)
                    .ToList();

            int i = 0;
            if (!usrs.Any())
                await channel.SendErrorAsync("Nobody is playing that game.").ConfigureAwait(false);
            else
                await channel.SendConfirmAsync($"List of users playing {game} game. Total {usrs.Count}.", "```css\n" + string.Join("\n", usrs.Take(30).GroupBy(item => (i++) / 2)
                                                                                 .Select(ig => string.Concat(ig.Select(el => $"• {el,-27}")))) + "\n```")
                                                                                 .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task InRole(IUserMessage umsg, [Remainder] string roles)
        {
            if (string.IsNullOrWhiteSpace(roles))
                return;
            var channel = (ITextChannel)umsg.Channel;
            var arg = roles.Split(',').Select(r => r.Trim().ToUpperInvariant());
            string send = "ℹ️ **Here is a list of users in those roles:**";
            foreach (var roleStr in arg.Where(str => !string.IsNullOrWhiteSpace(str) && str != "@EVERYONE" && str != "EVERYONE"))
            {
                var role = channel.Guild.Roles.Where(r => r.Name.ToUpperInvariant() == roleStr).FirstOrDefault();
                if (role == null) continue;
                send += $"```css\n[{role.Name}]\n";
                send += string.Join(", ", channel.Guild.GetUsers().Where(u => u.Roles.Contains(role)).Select(u => u.ToString()));
                send += $"\n```";
            }
            var usr = umsg.Author as IGuildUser;
            while (send.Length > 2000)
            {
                if (!usr.GetPermissions(channel).ManageMessages)
                {
                    await channel.SendErrorAsync($"⚠️ {usr.Mention} **you are not allowed to use this command on roles with a lot of users in them to prevent abuse.**").ConfigureAwait(false);
                    return;
                }
                var curstr = send.Substring(0, 2000);
                await channel.SendConfirmAsync(curstr.Substring(0,
                        curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1)).ConfigureAwait(false);
                send = curstr.Substring(curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1) +
                       send.Substring(2000);
            }
            await channel.SendConfirmAsync(send).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task CheckMyPerms(IUserMessage msg)
        {

            StringBuilder builder = new StringBuilder("```http\n");
            var user = msg.Author as IGuildUser;
            var perms = user.GetPermissions((ITextChannel)msg.Channel);
            foreach (var p in perms.GetType().GetProperties().Where(p => !p.GetGetMethod().GetParameters().Any()))
            {
                builder.AppendLine($"{p.Name} : {p.GetValue(perms, null).ToString()}");
            }

            builder.Append("```");
            await msg.Channel.SendConfirmAsync(builder.ToString());
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UserId(IUserMessage msg, IGuildUser target = null)
        {
            var usr = target ?? msg.Author;
            await msg.Channel.SendConfirmAsync($"🆔 of the user **{ usr.Username }** is `{ usr.Id }`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ChannelId(IUserMessage msg)
        {
            await msg.Channel.SendConfirmAsync($"🆔 of this channel is `{msg.Channel.Id}`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ServerId(IUserMessage msg)
        {
            await msg.Channel.SendConfirmAsync($"🆔 of this server is `{((ITextChannel)msg.Channel).Guild.Id}`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Roles(IUserMessage msg, IGuildUser target, int page = 1)
        {
            var channel = (ITextChannel)msg.Channel;
            var guild = channel.Guild;

            const int RolesPerPage = 20;

            if (page < 1 || page > 100)
                return;

            if (target != null)
            {
                var roles = target.Roles.Except(new[] { guild.EveryoneRole }).OrderBy(r => -r.Position).Skip((page - 1) * RolesPerPage).Take(RolesPerPage);
                if (!roles.Any())
                {
                    await channel.SendErrorAsync("No roles on this page.").ConfigureAwait(false);
                }
                else
                {
                    await channel.SendConfirmAsync($"⚔ **Page #{page} of roles for {target.Username}**", $"```css\n• " + string.Join("\n• ", roles).SanitizeMentions() + "\n```").ConfigureAwait(false);
                }
            }
            else
            {
                var roles = guild.Roles.Except(new[] { guild.EveryoneRole }).OrderBy(r => -r.Position).Skip((page - 1) * RolesPerPage).Take(RolesPerPage);
                if (!roles.Any())
                {
                    await channel.SendErrorAsync("No roles on this page.").ConfigureAwait(false);
                }
                else
                {
                    await channel.SendConfirmAsync($"⚔ **Page #{page} of all roles on this server:**", $"```css\n• " + string.Join("\n• ", roles).SanitizeMentions() + "\n```").ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Roles(IUserMessage msg, int page = 1) =>
            Roles(msg, null, page);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChannelTopic(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            var topic = channel.Topic;
            if (string.IsNullOrWhiteSpace(topic))
                await channel.SendErrorAsync("No topic set.");
            else
                await channel.SendConfirmAsync("Channel topic", topic);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Stats(IUserMessage umsg)
        {
            var channel = umsg.Channel;

            var stats = NadekoBot.Stats;

            await channel.EmbedAsync(
                new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithName($"NadekoBot v{StatsService.BotVersion}")
                                          .WithUrl("http://nadekobot.readthedocs.io/en/latest/")
                                          .WithIconUrl("https://cdn.discordapp.com/avatars/116275390695079945/b21045e778ef21c96d175400e779f0fb.jpg"))
                    .AddField(efb => efb.WithName(Format.Bold("Author")).WithValue(stats.Author).WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Library")).WithValue(stats.Library).WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Bot ID")).WithValue(NadekoBot.Client.GetCurrentUser().Id.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Commands Ran")).WithValue(stats.CommandsRan.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Messages")).WithValue($"{stats.MessageCounter} ({stats.MessagesPerSecond:F2}/sec)").WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Memory")).WithValue($"{stats.Heap} MB").WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Owner ID(s)")).WithValue(stats.OwnerIds).WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Uptime")).WithValue(stats.GetUptimeString("\n")).WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Presence")).WithValue($"{NadekoBot.Client.GetGuilds().Count} Servers\n{stats.TextChannels} Text Channels\n{stats.VoiceChannels} Voice Channels").WithIsInline(true))
                    .WithFooter(efb => efb.WithText($"Playing {Music.Music.MusicPlayers.Where(mp => mp.Value.CurrentSong != null).Count()} songs, {Music.Music.MusicPlayers.Sum(mp => mp.Value.Playlist.Count)} queued."))
                    .Build());
        }

        private Regex emojiFinder { get; } = new Regex(@"<:(?<name>.+?):(?<id>\d*)>", RegexOptions.Compiled);
        [NadekoCommand, Usage, Description, Aliases]
        public async Task Showemojis(IUserMessage msg, [Remainder] string emojis)
        {
            var matches = emojiFinder.Matches(emojis);

            var result = string.Join("\n", matches.Cast<Match>()
                                                  .Select(m => $"**Name:** {m.Groups["name"]} **Link:** http://discordapp.com/api/emojis/{m.Groups["id"]}.png"));

            if (string.IsNullOrWhiteSpace(result))
                await msg.Channel.SendErrorAsync("No special emojis found.");
            else
                await msg.Channel.SendMessageAsync(result).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task ListServers(IUserMessage imsg, int page = 1)
        {
            var channel = (ITextChannel)imsg.Channel;

            page -= 1;

            if (page < 0)
                return;

            var guilds = NadekoBot.Client.GetGuilds().OrderBy(g => g.Name).Skip((page - 1) * 15).Take(15);

            if (!guilds.Any())
            {
                await channel.SendErrorAsync("No servers found on that page.").ConfigureAwait(false);
                return;
            }

            await channel.EmbedAsync(guilds.Aggregate(new EmbedBuilder().WithOkColor(),
                                     (embed, g) => embed.AddField(efb => efb.WithName(g.Name)
                                                                           .WithValue($"```css\nID: {g.Id}\nMembers: {g.GetUsers().Count}\nOwnerID: {g.OwnerId} ```")
                                                                           .WithIsInline(false)))
                                           .Build())
                         .ConfigureAwait(false);
        }
    }
}