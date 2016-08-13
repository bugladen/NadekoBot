using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{

    [Module(".", AppendSpace = false)]
    public class UtilityModule
    {
        [LocalizedCommand]
        [LocalizedDescription]
        [LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task WhoPlays(IMessage imsg, [Remainder] string game)
        {
            var chnl = (IGuildChannel)imsg.Channel;
            game = game.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(game))
                return;
            var arr = (await chnl.Guild.GetUsersAsync())
                    .Where(u => u.Game?.Name.ToUpperInvariant() == game)
                    .Select(u => u.Username)
                    .ToList();

            int i = 0;
            if (!arr.Any())
                await imsg.Channel.SendMessageAsync("`Noone is playing that game.`").ConfigureAwait(false);
            else
                await imsg.Channel.SendMessageAsync("```xl\n" + string.Join("\n", arr.GroupBy(item => (i++) / 3).Select(ig => string.Concat(ig.Select(el => $"• {el,-35}")))) + "\n```").ConfigureAwait(false);
        }

        [LocalizedCommand]
        [LocalizedDescription]
        [LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task InRole(IMessage imsg, [Remainder] string roles) {
            if (string.IsNullOrWhiteSpace(roles))
                return;
            var channel = imsg.Channel as IGuildChannel;
            var arg = roles.Split(',').Select(r => r.Trim().ToUpperInvariant());
            string send = $"`Here is a list of users in a specfic role:`";
            foreach (var roleStr in arg.Where(str => !string.IsNullOrWhiteSpace(str) && str != "@EVERYONE" && str != "EVERYONE"))
            {
                var role = channel.Guild.Roles.Where(r => r.Name.ToUpperInvariant() == roleStr).FirstOrDefault();
                if (role == null) continue;
                send += $"\n`{role.Name}`\n";
                send += string.Join(", ", (await channel.Guild.GetUsersAsync()).Where(u=>u.Roles.Contains(role)).Select(u => u.ToString()));
            }

            //todo


            //while (send.Length > 2000)
            //{
            //    if (!)
            //    {
            //        await e.Channel.SendMessage($"{e.User.Mention} you are not allowed to use this command on roles with a lot of users in them to prevent abuse.");
            //        return;
            //    }
            //    var curstr = send.Substring(0, 2000);
            //    await imsg.Channel.SendMessageAsync(curstr.Substring(0,
            //            curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1)).ConfigureAwait(false);
            //    send = curstr.Substring(curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1) +
            //           send.Substring(2000);
            //}
            //await e.Channel.Send(send).ConfigureAwait(false);
        }



    }
}
        //public void Install()
        //{
        //    manager.CreateCommands("", cgb =>
        //    {
        //        cgb.AddCheck(PermissionChecker.Instance);

        //        var client = manager.Client;

        //        commands.ForEach(cmd => cmd.Init(cgb));

        //        cgb.CreateCommand(Prefix + "whoplays")
        //            .Description()
        //            .Parameter("game", ParameterType.Unparsed)
        //            .Do(async e =>
        //            {
                        
        //            });

        

        //        cgb.CreateCommand(Prefix + "checkmyperms")
        //            .Description($"Checks your userspecific permissions on this channel. | `{Prefix}checkmyperms`")
        //            .Do(async e =>
        //            {
        //                var output = "```\n";
        //                foreach (var p in e.User.ServerPermissions.GetType().GetProperties().Where(p => !p.GetGetMethod().GetParameters().Any()))
        //                {
        //                    output += p.Name + ": " + p.GetValue(e.User.ServerPermissions, null).ToString() + "\n";
        //                }
        //                output += "```";
        //                await e.User.SendMessage(output).ConfigureAwait(false);
        //            });

        //        cgb.CreateCommand(Prefix + "stats")
        //            .Description($"Shows some basic stats for Nadeko. | `{Prefix}stats`")
        //            .Do(async e =>
        //            {
        //                await e.Channel.SendMessage(await NadekoStats.Instance.GetStats()).ConfigureAwait(false);
        //            });

        //        cgb.CreateCommand(Prefix + "dysyd")
        //            .Description($"Shows some basic stats for Nadeko. | `{Prefix}dysyd`")
        //            .Do(async e =>
        //            {
        //                await e.Channel.SendMessage((await NadekoStats.Instance.GetStats()).Matrix().TrimTo(1990)).ConfigureAwait(false);
        //            });

        //        cgb.CreateCommand(Prefix + "userid").Alias(Prefix + "uid")
        //            .Description($"Shows user ID. | `{Prefix}uid` or `{Prefix}uid \"@SomeGuy\"`")
        //            .Parameter("user", ParameterType.Unparsed)
        //            .Do(async e =>
        //            {
        //                var usr = e.User;
        //                if (!string.IsNullOrWhiteSpace(e.GetArg("user"))) usr = e.Channel.FindUsers(e.GetArg("user")).FirstOrDefault();
        //                if (usr == null)
        //                    return;
        //                await e.Channel.SendMessage($"Id of the user { usr.Name } is { usr.Id }").ConfigureAwait(false);
        //            });

        //        cgb.CreateCommand(Prefix + "channelid").Alias(Prefix + "cid")
        //            .Description($"Shows current channel ID. | `{Prefix}cid`")
        //            .Do(async e => await e.Channel.SendMessage("This channel's ID is " + e.Channel.Id).ConfigureAwait(false));

        //        cgb.CreateCommand(Prefix + "serverid").Alias(Prefix + "sid")
        //            .Description($"Shows current server ID. | `{Prefix}sid`")
        //            .Do(async e => await e.Channel.SendMessage("This server's ID is " + e.Server.Id).ConfigureAwait(false));

        //        cgb.CreateCommand(Prefix + "roles")
        //            .Description("List all roles on this server or a single user if specified. | `{Prefix}roles`")
        //            .Parameter("user", ParameterType.Unparsed)
        //            .Do(async e =>
        //            {
        //                if (!string.IsNullOrWhiteSpace(e.GetArg("user")))
        //                {
        //                    var usr = e.Server.FindUsers(e.GetArg("user")).FirstOrDefault();
        //                    if (usr == null) return;

        //                    await e.Channel.SendMessage($"`List of roles for **{usr.Name}**:` \n• " + string.Join("\n• ", usr.Roles)).ConfigureAwait(false);
        //                    return;
        //                }
        //                await e.Channel.SendMessage("`List of roles:` \n• " + string.Join("\n• ", e.Server.Roles)).ConfigureAwait(false);
        //            });


        //        cgb.CreateCommand(Prefix + "channeltopic")
        //            .Alias(Prefix + "ct")
        //            .Description($"Sends current channel's topic as a message. | `{Prefix}ct`")
        //            .Do(async e =>
        //            {
        //                var topic = e.Channel.Topic;
        //                if (string.IsNullOrWhiteSpace(topic))
        //                    return;
        //                await e.Channel.SendMessage(topic).ConfigureAwait(false);
        //            });
        //    });
//        }
//    }
//}

