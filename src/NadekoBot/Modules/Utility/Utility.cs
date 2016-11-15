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

namespace NadekoBot.Modules.Utility
{

    [NadekoModule("Utility", ".")]
    public partial class Utility : DiscordModule
    {
        public Utility(ILocalization loc, CommandService cmds, ShardedDiscordClient client) : base(loc, cmds, client)
        {

        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task WhosPlaying(IUserMessage umsg, [Remainder] string game = null)
        {
            var channel = (ITextChannel)umsg.Channel;
            game = game.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(game))
                return;
            var arr = (await (umsg.Channel as IGuildChannel).Guild.GetUsersAsync())
                    .Where(u => u.Game?.Name?.ToUpperInvariant() == game)
                    .Select(u => u.Username)
                    .ToList();

            int i = 0;
            if (!arr.Any())
                await channel.SendMessageAsync(_l["`Nobody is playing that game.`"]).ConfigureAwait(false);
            else
                await channel.SendMessageAsync("```xl\n" + string.Join("\n", arr.GroupBy(item => (i++) / 3).Select(ig => string.Concat(ig.Select(el => $"• {el,-35}")))) + "\n```").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task InRole(IUserMessage umsg, [Remainder] string roles)
        {
            if (string.IsNullOrWhiteSpace(roles))
                return;
            var channel = (ITextChannel)umsg.Channel;
            var arg = roles.Split(',').Select(r => r.Trim().ToUpperInvariant());
            string send = _l["`Here is a list of users in a specfic role:`"];
            foreach (var roleStr in arg.Where(str => !string.IsNullOrWhiteSpace(str) && str != "@EVERYONE" && str != "EVERYONE"))
            {
                var role = channel.Guild.Roles.Where(r => r.Name.ToUpperInvariant() == roleStr).FirstOrDefault();
                if (role == null) continue;
                send += $"\n`{role.Name}`\n";
                send += string.Join(", ", channel.Guild.GetUsers().Where(u => u.Roles.Contains(role)).Select(u => u.ToString()));
            }
            var usr = umsg.Author as IGuildUser;
            while (send.Length > 2000)
            {
                if (!usr.GetPermissions(channel).ManageMessages)
                {
                    await channel.SendMessageAsync($"{usr.Mention} you are not allowed to use this command on roles with a lot of users in them to prevent abuse.").ConfigureAwait(false);
                    return;
                }
                var curstr = send.Substring(0, 2000);
                await channel.SendMessageAsync(curstr.Substring(0,
                        curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1)).ConfigureAwait(false);
                send = curstr.Substring(curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1) +
                       send.Substring(2000);
            }
            await channel.SendMessageAsync(send).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task CheckMyPerms(IUserMessage msg)
        {

            StringBuilder builder = new StringBuilder("```\n");
            var user = msg.Author as IGuildUser;
            var perms = user.GetPermissions((ITextChannel)msg.Channel);
            foreach (var p in perms.GetType().GetProperties().Where(p => !p.GetGetMethod().GetParameters().Any()))
            {
                builder.AppendLine($"{p.Name} : {p.GetValue(perms, null).ToString()}");
            }

            builder.Append("```");
            await msg.Reply(builder.ToString());
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task UserId(IUserMessage msg, IGuildUser target = null)
        {
            var usr = target ?? msg.Author;
            await msg.Reply($"Id of the user { usr.Username } is { usr.Id }").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task ChannelId(IUserMessage msg)
        {
            await msg.Reply($"This Channel's ID is {msg.Channel.Id}").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task ServerId(IUserMessage msg)
        {
            await msg.Reply($"This server's ID is {((ITextChannel)msg.Channel).Guild.Id}").ConfigureAwait(false);
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
                await msg.Reply($"`Page #{page} of roles for **{target.Username}**:` \n• " + string.Join("\n• ", target.Roles.Except(new[] { guild.EveryoneRole }).OrderBy(r => r.Position).Skip((page - 1) * RolesPerPage).Take(RolesPerPage)).SanitizeMentions());
            }
            else
            {
                await msg.Reply($"`Page #{page} of all roles on this server:` \n• " + string.Join("\n• ", guild.Roles.Except(new[] { guild.EveryoneRole }).OrderBy(r => r.Position).Skip((page - 1) * RolesPerPage).Take(RolesPerPage)).SanitizeMentions());
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
                await channel.SendMessageAsync("`No topic set.`");
            else
                await channel.SendMessageAsync("`Topic:` " + topic);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Stats(IUserMessage umsg)
        {
            var channel = umsg.Channel;

            await channel.SendMessageAsync(await NadekoBot.Stats.Print());
        }

        private Regex emojiFinder { get; } = new Regex(@"<:(?<name>.+?):(?<id>\d*)>", RegexOptions.Compiled);
        [NadekoCommand, Usage, Description, Aliases]
        public async Task Showemojis(IUserMessage msg, [Remainder] string emojis)
        {
            var matches = emojiFinder.Matches(emojis);

            var result = string.Join("\n", matches.Cast<Match>()
                                                  .Select(m => $"`Name:` {m.Groups["name"]} `Link:` http://discordapp.com/api/emojis/{m.Groups["id"]}.png"));
            
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
                await channel.SendMessageAsync("`No servers found on that page.`").ConfigureAwait(false);
                return;
            }

            await channel.SendMessageAsync(String.Join("\n", guilds.Select(g => $"`Name:` {g.Name} `Id:` {g.Id} `Members:` {g.GetUsers().Count} `OwnerId:`{g.OwnerId}"))).ConfigureAwait(false);
        }

        //[NadekoCommand, Usage, Description, Aliases]
        //[RequireContext(ContextType.Guild)]
        //public async Task TextToImage(IUserMessage msg, [Remainder] string arg)
        //{
        //    var channel = (ITextChannel)msg.Channel;

        //    const string bgName = "xbiy3";

        //    if (string.IsNullOrWhiteSpace(arg))
        //        return;

        //    using (var http = new HttpClient())
        //    {
        //        http.AddFakeHeaders();

        //        http.DefaultRequestHeaders.Add("Host", "www.tagsmaker.com");
        //        http.DefaultRequestHeaders.Add("Referer", "http://www.tagsmaker.com/");
        //        http.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        //        http.DefaultRequestHeaders.Add("Alt-Used", "www.tagsmaker.com:443");

        //        var res = await http.GetAsync($"http://www.tagsmaker.com/tagsmaker.php?background_name=0011&tag_text={arg}&font_name=applejuiced&text_color=white&text_size=48&text_alignment=middle").ConfigureAwait(false);

        //        var img = res.RequestMessage.RequestUri.Segments[1].Replace("image-", "").Replace("tag-", "");
        //        var imgStream = await http.GetStreamAsync($"http://www.tagsmaker.com/upload/www.tagsmaker.com_{ img.ToString() }.png");
        //        var ms = new MemoryStream();
        //        await imgStream.CopyToAsync(ms).ConfigureAwait(false);
        //        ms.Position = 0;
        //        await channel.SendFileAsync(ms, arg+".png", "Provided by www.tagsmaker.com").ConfigureAwait(false);
        //    }
        //}
    }
}

