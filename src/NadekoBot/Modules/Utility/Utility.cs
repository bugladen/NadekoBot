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
using System.Collections.Generic;
using System.Reflection;

namespace NadekoBot.Modules.Utility
{

    [Module(".", AppendSpace = false)]
    public partial class Utility : DiscordModule
    {
        public Utility(ILocalization loc, CommandService cmds, IBotConfiguration config, IDiscordClient client) : base(loc, cmds, config, client)
        {

        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task WhoPlays(IMessage imsg, [Remainder] string game = null)
        {
            var chnl = (IGuildChannel)imsg.Channel;
            game = game.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(game))
                return;
            var arr = (await chnl.Guild.GetUsersAsync())
                    .Where(u => u.Game?.Name?.ToUpperInvariant() == game)
                    .Select(u => u.Username)
                    .ToList();

            int i = 0;
            if (!arr.Any())
                await channel.SendMessageAsync(_l["`Nobody is playing that game.`"]).ConfigureAwait(false);
            else
                await channel.SendMessageAsync("```xl\n" + string.Join("\n", arr.GroupBy(item => (i++) / 3).Select(ig => string.Concat(ig.Select(el => $"• {el,-35}")))) + "\n```").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task InRole(IMessage imsg, [Remainder] string roles = null)
        {
            if (string.IsNullOrWhiteSpace(roles))
                return;
            var channel = imsg.Channel as ITextChannel;
            var arg = roles.Split(',').Select(r => r.Trim().ToUpperInvariant());
            string send = _l["`Here is a list of users in a specfic role:`"];
            foreach (var roleStr in arg.Where(str => !string.IsNullOrWhiteSpace(str) && str != "@EVERYONE" && str != "EVERYONE"))
            {
                var role = channel.Guild.Roles.Where(r => r.Name.ToUpperInvariant() == roleStr).FirstOrDefault();
                if (role == null) continue;
                send += $"\n`{role.Name}`\n";
                send += string.Join(", ", (await channel.Guild.GetUsersAsync()).Where(u => u.Roles.Contains(role)).Select(u => u.ToString()));
            }
            var usr = imsg.Author as IGuildUser;
            while (send.Length > 2000)
            {
                if (!usr.GetPermissions(channel).ManageMessages)
                {
                    await channel.SendMessageAsync($"{usr.Mention} you are not allowed to use this command on roles with a lot of users in them to prevent abuse.");
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

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task CheckMyPerms(IMessage msg)
        {

            StringBuilder builder = new StringBuilder("```\n");
            var user = msg.Author as IGuildUser;
            var perms = user.GetPermissions(msg.Channel as ITextChannel);
            foreach (var p in perms.GetType().GetProperties().Where(p => !p.GetGetMethod().GetParameters().Any()))
            {
                builder.AppendLine($"{p.Name} : {p.GetValue(perms, null).ToString()}");
            }

            builder.Append("```");
            await msg.Reply(builder.ToString());
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task UserId(IMessage msg, IGuildUser target = null)
        {
            var usr = target ?? msg.Author;
            await msg.Reply($"Id of the user { usr.Username } is { usr.Id })");
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        public async Task ChannelId(IMessage msg)
        {
            await msg.Reply($"This Channel's ID is {msg.Channel.Id}");
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task ServerId(IMessage msg)
        {
            await msg.Reply($"This server's ID is {(msg.Channel as ITextChannel).Guild.Id}");
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Roles(IMessage msg, IGuildUser target = null)
        {
            var guild = (msg.Channel as ITextChannel).Guild;
            if (target != null)
            {
                await msg.Reply($"`List of roles for **{target.Username}**:` \n• " + string.Join("\n• ", target.Roles.Except(new[] { guild.EveryoneRole })));
            }
            else
            {
                await msg.Reply("`List of roles:` \n• " + string.Join("\n• ", (msg.Channel as ITextChannel).Guild.Roles.Except(new[] { guild.EveryoneRole })));
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Stats(IMessage imsg)
        {
            var channel = imsg.Channel as ITextChannel;

            await channel.SendMessageAsync(await NadekoBot.Stats.Print());
        }
    }
}

