using Discord.Commands;
using Discord.Modules;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Classes;
using NadekoBot.Modules.Utility.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    internal class UtilityModule : DiscordModule
    {
        public UtilityModule()
        {
            commands.Add(new Remind(this));
            commands.Add(new InfoCommands(this));
        }

        public override string Prefix => NadekoBot.Config.CommandPrefixes.Utility;

        public override void Install(ModuleManager manager)
        {

            manager.CreateCommands("", cgb =>
            {
                cgb.AddCheck(PermissionChecker.Instance);

                var client = manager.Client;

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(Prefix + "whoplays")
                    .Description($"Shows a list of users who are playing the specified game. | `{Prefix}whoplays Overwatch`")
                    .Parameter("game", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var game = e.GetArg("game")?.Trim().ToUpperInvariant();
                        if (string.IsNullOrWhiteSpace(game))
                            return;
                        var en = e.Server.Users
                                .Where(u => u.CurrentGame?.Name?.ToUpperInvariant() == game)
                                .Select(u => u.Name);

                        var arr = en as string[] ?? en.ToArray();

                        int i = 0;
                        if (arr.Length == 0)
                            await e.Channel.SendMessage("Nobody. (not 100% sure)").ConfigureAwait(false);
                        else
                            await e.Channel.SendMessage("```xl\n" + string.Join("\n", arr.GroupBy(item => (i++) / 3).Select(ig => string.Concat(ig.Select(el => $"• {el,-35}")))) + "\n```").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "inrole")
                    .Description($"Lists every person from the provided role or roles (separated by a ',') on this server. If the list is too long for 1 message, you must have Manage Messages permission. | `{Prefix}inrole Role`")
                    .Parameter("roles", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await Task.Run(async () =>
                        {
                            var arg = e.GetArg("roles").Split(',').Select(r => r.Trim());
                            string send = $"`Here is a list of users in a specfic role:`";
                            foreach (var roleStr in arg.Where(str => !string.IsNullOrWhiteSpace(str) && str != "@everyone" && str != "everyone"))
                            {
                                var role = e.Server.FindRoles(roleStr).FirstOrDefault();
                                if (role == null) continue;
                                send += $"\n`{role.Name}`\n";
                                send += string.Join(", ", role.Members.Select(r => "**" + r.Name + "**#" + r.Discriminator));
                            }

                            while (send.Length > 2000)
                            {
                                if (!e.User.ServerPermissions.ManageMessages)
                                {
                                    await e.Channel.SendMessage($"{e.User.Mention} you are not allowed to use this command on roles with a lot of users in them to prevent abuse.");
                                    return;
                                }
                                var curstr = send.Substring(0, 2000);
                                await
                                    e.Channel.Send(curstr.Substring(0,
                                        curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1)).ConfigureAwait(false);
                                send = curstr.Substring(curstr.LastIndexOf(", ", StringComparison.Ordinal) + 1) +
                                       send.Substring(2000);
                            }
                            await e.Channel.Send(send).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "checkmyperms")
                    .Description($"Checks your userspecific permissions on this channel. | `{Prefix}checkmyperms`")
                    .Do(async e =>
                    {
                        var output = "```\n";
                        foreach (var p in e.User.ServerPermissions.GetType().GetProperties().Where(p => !p.GetGetMethod().GetParameters().Any()))
                        {
                            output += p.Name + ": " + p.GetValue(e.User.ServerPermissions, null).ToString() + "\n";
                        }
                        output += "```";
                        await e.User.SendMessage(output).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "stats")
                    .Description($"Shows some basic stats for Nadeko. | `{Prefix}stats`")
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(await NadekoStats.Instance.GetStats()).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "dysyd")
                    .Description($"Shows some basic stats for Nadeko. | `{Prefix}dysyd`")
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage((await NadekoStats.Instance.GetStats()).Matrix().TrimTo(1990)).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "userid").Alias(Prefix + "uid")
                    .Description($"Shows user ID. | `{Prefix}uid` or `{Prefix}uid \"@SomeGuy\"`")
                    .Parameter("user", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var usr = e.User;
                        if (!string.IsNullOrWhiteSpace(e.GetArg("user"))) usr = e.Channel.FindUsers(e.GetArg("user")).FirstOrDefault();
                        if (usr == null)
                            return;
                        await e.Channel.SendMessage($"Id of the user { usr.Name } is { usr.Id }").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "channelid").Alias(Prefix + "cid")
                    .Description($"Shows current channel ID. | `{Prefix}cid`")
                    .Do(async e => await e.Channel.SendMessage("This channel's ID is " + e.Channel.Id).ConfigureAwait(false));

                cgb.CreateCommand(Prefix + "serverid").Alias(Prefix + "sid")
                    .Description($"Shows current server ID. | `{Prefix}sid`")
                    .Do(async e => await e.Channel.SendMessage("This server's ID is " + e.Server.Id).ConfigureAwait(false));

                cgb.CreateCommand(Prefix + "roles")
                    .Description("List all roles on this server or a single user if specified.")
                    .Parameter("user", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.GetArg("user")))
                        {
                            var usr = e.Server.FindUsers(e.GetArg("user")).FirstOrDefault();
                            if (usr == null) return;

                            await e.Channel.SendMessage($"`List of roles for **{usr.Name}**:` \n• " + string.Join("\n• ", usr.Roles)).ConfigureAwait(false);
                            return;
                        }
                        await e.Channel.SendMessage("`List of roles:` \n• " + string.Join("\n• ", e.Server.Roles)).ConfigureAwait(false);
                    });


                cgb.CreateCommand(Prefix + "channeltopic")
                    .Alias(Prefix + "ct")
                    .Description($"Sends current channel's topic as a message. | `{Prefix}ct`")
                    .Do(async e =>
                    {
                        var topic = e.Channel.Topic;
                        if (string.IsNullOrWhiteSpace(topic))
                            return;
                        await e.Channel.SendMessage(topic).ConfigureAwait(false);
                    });
            });
        }
    }
}

