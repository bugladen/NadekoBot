using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using NadekoBot.Extensions;
using System.Text.RegularExpressions;
using System.Reflection;
using NadekoBot.Services.Impl;
using System.Net.Http;
using System.Collections.Concurrent;
using System.Threading;
using ImageSharp;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Utility
{
    [NadekoModule("Utility", ".")]
    public partial class Utility : DiscordModule
    {
        private static ConcurrentDictionary<ulong, Timer> rotatingRoleColors = new ConcurrentDictionary<ulong, Timer>();

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task RotateRoleColor(int timeout, IRole role, params string[] hexes)
        {
            var channel = (ITextChannel)Context.Channel;

            if ((timeout < 60 && timeout != 0) || timeout > 3600)
                return;

            Timer t;
            if (timeout == 0 || hexes.Length == 0)
            {
                if (rotatingRoleColors.TryRemove(role.Id, out t))
                {
                    t.Change(Timeout.Infinite, Timeout.Infinite);
                    await channel.SendConfirmAsync($"Stopped rotating colors for the **{role.Name}** role").ConfigureAwait(false);
                }
                return;
            }

            var hexColors = hexes.Select(hex =>
            {
                try { return (ImageSharp.Color?)new ImageSharp.Color(hex.Replace("#", "")); } catch { return null; }
            })
            .Where(c => c != null)
            .Select(c => c.Value)
            .ToArray();

            if (!hexColors.Any())
            {
                await channel.SendMessageAsync("No colors are in the correct format. Use `#00ff00` for example.").ConfigureAwait(false);
                return;
            }

            var images = hexColors.Select(color =>
            {
                var img = new ImageSharp.Image(50, 50);
                img.BackgroundColor(color);
                return img;
            }).Merge().ToStream();

            var i = 0;
            t = new Timer(async (_) =>
            {
                try
                {
                    var color = hexColors[i];
                    await role.ModifyAsync(r => r.Color = new Discord.Color(color.R, color.G, color.B)).ConfigureAwait(false);
                    ++i;
                    if (i >= hexColors.Length)
                        i = 0;
                }
                catch { }
            }, null, 0, timeout * 1000);

            rotatingRoleColors.AddOrUpdate(role.Id, t, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

            await channel.SendFileAsync(images, "magicalgirl.jpg", $"Rotating **{role.Name}** role's color.").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task TogetherTube()
        {
            Uri target;
            using (var http = new HttpClient())
            {
                var res = await http.GetAsync("https://togethertube.com/room/create").ConfigureAwait(false);
                target = res.RequestMessage.RequestUri;
            }

            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithAuthor(eab => eab.WithIconUrl("https://togethertube.com/assets/img/favicons/favicon-32x32.png")
                .WithName("Together Tube")
                .WithUrl("https://togethertube.com/"))
                .WithDescription($"{Context.User.Mention} Here is your room link:\n{target}"));
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task WhosPlaying([Remainder] string game = null)
        {
            game = game.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(game))
                return;
            var arr = (await (Context.Channel as IGuildChannel).Guild.GetUsersAsync())
                    .Where(u => u.Game?.Name?.ToUpperInvariant() == game)
                    .Select(u => u.Username)
                    .ToList();

            int i = 0;
            if (!arr.Any())
                await Context.Channel.SendErrorAsync("Nobody is playing that game.").ConfigureAwait(false);
            else
                await Context.Channel.SendConfirmAsync("```css\n" + string.Join("\n", arr.GroupBy(item => (i++) / 2)
                                                                                 .Select(ig => string.Concat(ig.Select(el => $"• {el,-27}")))) + "\n```")
                                                                                 .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task InRole([Remainder] string roles)
        {
            if (string.IsNullOrWhiteSpace(roles))
                return;
            var arg = roles.Split(',').Select(r => r.Trim().ToUpperInvariant());
            string send = "ℹ️ **Here is a list of users in those roles:**";
            foreach (var roleStr in arg.Where(str => !string.IsNullOrWhiteSpace(str) && str != "@EVERYONE" && str != "EVERYONE"))
            {
                var role = Context.Guild.Roles.Where(r => r.Name.ToUpperInvariant() == roleStr).FirstOrDefault();
                if (role == null) continue;
                send += $"```css\n[{role.Name}]\n";
                send += string.Join(", ", (await Context.Guild.GetUsersAsync()).Where(u => u.RoleIds.Contains(role.Id)).Select(u => u.ToString()));
                send += $"\n```";
            }
            var usr = Context.User as IGuildUser;
            while (send.Length > 2000)
            {
                if (!usr.GetPermissions((ITextChannel)Context.Channel).ManageMessages)
                {
                    await Context.Channel.SendErrorAsync($"⚠️ {usr.Mention} **you are not allowed to use this command on roles with a lot of users in them to prevent abuse.**").ConfigureAwait(false);
                    return;
                }
                var curstr = send.Substring(0, 2000);
                await Context.Channel.SendConfirmAsync(curstr.Substring(0,
                        curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1)).ConfigureAwait(false);
                send = curstr.Substring(curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1) +
                       send.Substring(2000);
            }
            await Context.Channel.SendConfirmAsync(send).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task CheckMyPerms()
        {

            StringBuilder builder = new StringBuilder("```http\n");
            var user = Context.User as IGuildUser;
            var perms = user.GetPermissions((ITextChannel)Context.Channel);
            foreach (var p in perms.GetType().GetProperties().Where(p => !p.GetGetMethod().GetParameters().Any()))
            {
                builder.AppendLine($"{p.Name} : {p.GetValue(perms, null).ToString()}");
            }

            builder.Append("```");
            await Context.Channel.SendConfirmAsync(builder.ToString());
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UserId(IGuildUser target = null)
        {
            var usr = target ?? Context.User;
            await Context.Channel.SendConfirmAsync($"🆔 of the user **{ usr.Username }** is `{ usr.Id }`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ChannelId()
        {
            await Context.Channel.SendConfirmAsync($"🆔 of this channel is `{Context.Channel.Id}`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ServerId()
        {
            await Context.Channel.SendConfirmAsync($"🆔 of this server is `{Context.Guild.Id}`").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Roles(IGuildUser target, int page = 1)
        {
            var channel = (ITextChannel)Context.Channel;
            var guild = channel.Guild;

            const int RolesPerPage = 20;

            if (page < 1 || page > 100)
                return;

            if (target != null)
            {
                var roles = target.GetRoles().Except(new[] { guild.EveryoneRole }).OrderBy(r => -r.Position).Skip((page - 1) * RolesPerPage).Take(RolesPerPage);
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
        public Task Roles(int page = 1) =>
            Roles(null, page);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ChannelTopic([Remainder]ITextChannel channel = null)
        {
            if (channel == null)
                channel = (ITextChannel)Context.Channel;

            var topic = channel.Topic;
            if (string.IsNullOrWhiteSpace(topic))
                await Context.Channel.SendErrorAsync("No topic set.").ConfigureAwait(false);
            else
                await Context.Channel.SendConfirmAsync("Channel topic", topic).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(ChannelPermission.CreateInstantInvite)]
        [RequireUserPermission(ChannelPermission.CreateInstantInvite)]
        public async Task CreateInvite()
        {
            var invite = await ((ITextChannel)Context.Channel).CreateInviteAsync(0, null, isUnique: true);

            await Context.Channel.SendConfirmAsync($"{Context.User.Mention} https://discord.gg/{invite.Code}");
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Stats()
        {
            var stats = NadekoBot.Stats;

            await Context.Channel.EmbedAsync(
                new EmbedBuilder().WithOkColor()
                    .WithAuthor(eab => eab.WithName($"NadekoBot v{StatsService.BotVersion}")
                                          .WithUrl("http://nadekobot.readthedocs.io/en/latest/")
                                          .WithIconUrl("https://cdn.discordapp.com/avatars/116275390695079945/b21045e778ef21c96d175400e779f0fb.jpg"))
                    .AddField(efb => efb.WithName(Format.Bold("Author")).WithValue(stats.Author).WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Library")).WithValue(stats.Library).WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Bot ID")).WithValue(NadekoBot.Client.CurrentUser.Id.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Commands Ran")).WithValue(stats.CommandsRan.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Messages")).WithValue($"{stats.MessageCounter} ({stats.MessagesPerSecond:F2}/sec)").WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Memory")).WithValue($"{stats.Heap} MB").WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Owner ID(s)")).WithValue(string.Join("\n", NadekoBot.Credentials.OwnerIds)).WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Uptime")).WithValue(stats.GetUptimeString("\n")).WithIsInline(true))
                    .AddField(efb => efb.WithName(Format.Bold("Presence")).WithValue($"{NadekoBot.Client.GetGuildCount()} Servers\n{stats.TextChannels} Text Channels\n{stats.VoiceChannels} Voice Channels").WithIsInline(true))
#if !GLOBAL_NADEKO
                    .WithFooter(efb => efb.WithText($"Playing {Music.Music.MusicPlayers.Where(mp => mp.Value.CurrentSong != null).Count()} songs, {Music.Music.MusicPlayers.Sum(mp => mp.Value.Playlist.Count)} queued."))
#endif
                    );
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Showemojis([Remainder] string emojis)
        {
            var tags = Context.Message.Tags.Where(t => t.Type == TagType.Emoji).Select(t => (Emoji)t.Value);

            var result = string.Join("\n", tags.Select(m => $"**Name:** {m} **Link:** {m.Url}"));

            if (string.IsNullOrWhiteSpace(result))
                await Context.Channel.SendErrorAsync("No special emojis found.");
            else
                await Context.Channel.SendMessageAsync(result).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task ListServers(int page = 1)
        {
            page -= 1;

            if (page < 0)
                return;

            var guilds = await Task.Run(() => NadekoBot.Client.GetGuilds().OrderBy(g => g.Name).Skip((page - 1) * 15).Take(15)).ConfigureAwait(false);

            if (!guilds.Any())
            {
                await Context.Channel.SendErrorAsync("No servers found on that page.").ConfigureAwait(false);
                return;
            }

            await Context.Channel.EmbedAsync(guilds.Aggregate(new EmbedBuilder().WithOkColor(),
                                     (embed, g) => embed.AddField(efb => efb.WithName(g.Name)
                                                                           .WithValue($"```css\nID: {g.Id}\nMembers: {g.Users.Count}\nOwnerID: {g.OwnerId} ```")
                                                                           .WithIsInline(false))))
                         .ConfigureAwait(false);
        }


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task SaveChat(int cnt)
        {
            var sb = new StringBuilder();
            var msgs = new List<IMessage>(cnt);
            await Context.Channel.GetMessagesAsync(cnt).ForEachAsync(dled => msgs.AddRange(dled)).ConfigureAwait(false);

            var title = $"Chatlog-{Context.Guild.Name}/#{Context.Channel.Name}-{DateTime.Now}.txt";
            var grouping = msgs.GroupBy(x => $"{x.CreatedAt.Date:dd.MM.yyyy}")
                .Select(g => new { date = g.Key, messages = g.OrderBy(x => x.CreatedAt).Select(s => $"【{s.Timestamp:HH:mm:ss}】{s.Author}:" + s.ToString()) });
            await Context.User.SendFileAsync(
                await JsonConvert.SerializeObject(grouping, Formatting.Indented).ToStream().ConfigureAwait(false), title, title).ConfigureAwait(false);
        }
    }
}