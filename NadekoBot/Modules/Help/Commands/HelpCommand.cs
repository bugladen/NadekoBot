//
//                       _oo0oo_
//                      o8888888o
//                      88" . "88
//                      (| -_- |)
//                      0\  =  /0
//                    ___/`---'\___
//                  .' \\|     |// '.
//                 / \\|||  :  |||// \
//                / _||||| -:- |||||- \
//               |   | \\\  -  /// |   |
//               | \_|  ''\---/''  |_/ |
//               \  .-\__  '-'  ___/-. /
//             ___'. .'  /--.--\  `. .'___
//          ."" '<  `.___\_<|>_/___.' >' "".
//         | | :  `- \`.;`\ _ /`;.`/ - ` : | |
//         \  \ `_.   \_ __\ /__ _/   .-` /  /
//     =====`-.____`.___ \_____/___.-`___.-'=====
//                       `=---='
//
//
//     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//
//               佛祖保佑         永无BUG
//
//
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Modules;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Classes.Help.Commands
{
    internal class HelpCommand : DiscordCommand
    {
        public Func<CommandEventArgs, Task> HelpFunc() => async e =>
        {
            var comToFind = e.GetArg("command")?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(comToFind))
            {
                await e.User.Send(HelpString).ConfigureAwait(false);
                return;
            }
            await Task.Run(async () =>
            {
                var com = NadekoBot.Client.GetService<CommandService>().AllCommands
                    .FirstOrDefault(c => c.Text.ToLowerInvariant().Equals(comToFind) ||
                                        c.Aliases.Select(a => a.ToLowerInvariant()).Contains(comToFind));

                var str = "";
                var alias = com.Aliases.FirstOrDefault();
                if (alias != null)
                    str = $" / `{ com.Aliases.FirstOrDefault()}`";
                if (com != null)
                    await e.Channel.SendMessage($@"**__Help for:__ `{com.Text}`**" + str + $"\n**Desc:** {new Regex(@"\|").Replace(com.Description, "\n**Usage:**", 1)}").ConfigureAwait(false);
            }).ConfigureAwait(false);
        };
        public static string HelpString {
            get {
                var str = !string.IsNullOrWhiteSpace(NadekoBot.Creds.ClientId) && !NadekoBot.Config.DontJoinServers
                    ? String.Format("To add me to your server, use this link -> <https://discordapp.com/oauth2/authorize?client_id={0}&scope=bot&permissions=66186303>\n", NadekoBot.Creds.ClientId)
                    : "";
                return str + String.Format(NadekoBot.Config.HelpString, NadekoBot.Config.CommandPrefixes.Help);
            }
        }

        public static string DMHelpString => NadekoBot.Config.DMHelpString;

        public Action<CommandEventArgs> DoGitFunc() => e =>
        {
            var helpstr = new StringBuilder();

            var lastCategory = "";
            foreach (var com in NadekoBot.Client.GetService<CommandService>().AllCommands)
            {
                if (com.Category != lastCategory)
                {
                    helpstr.AppendLine("\n### " + com.Category + "  ");
                    helpstr.AppendLine("Command and aliases | Description | Usage");
                    helpstr.AppendLine("----------------|--------------|-------");
                    lastCategory = com.Category;
                }
                helpstr.AppendLine($"`{com.Text}`{string.Concat(com.Aliases.Select(a => $", `{a}`"))} | {com.Description}");
            }
            helpstr = helpstr.Replace(NadekoBot.BotMention, "@BotName");
#if DEBUG
            File.WriteAllText("../../../docs/Commands List.md", helpstr.ToString());
#else
            File.WriteAllText("commandlist.md", helpstr.ToString());
#endif
        };

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "h")
                .Alias(Module.Prefix + "help", NadekoBot.BotMention + " help", NadekoBot.BotMention + " h", "~h")
                .Description($"Either shows a help for a single command, or PMs you help link if no arguments are specified. | `{Prefix}h !m q` or just `{Prefix}h` ")
                .Parameter("command", ParameterType.Unparsed)
                .Do(HelpFunc());
            cgb.CreateCommand(Module.Prefix + "hgit")
                .Description($"Generates the commandlist.md file. **Bot Owner Only!** | `{Prefix}hgit`")
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(DoGitFunc());
            cgb.CreateCommand(Module.Prefix + "readme")
                .Alias(Module.Prefix + "guide")
                .Description($"Sends a readme and a guide links to the channel. | `{Prefix}readme` or `{Prefix}guide`")
                .Do(async e =>
                    await e.Channel.SendMessage(
@"**LIST OF COMMANDS**: <http://nadekobot.readthedocs.io/en/latest/Commands%20List/>
**Hosting Guides and docs can be found here**: <http://nadekobot.rtfd.io>").ConfigureAwait(false));

            cgb.CreateCommand(Module.Prefix + "donate")
                .Alias("~donate")
                .Description($"Instructions for helping the project! | `{Prefix}donate` or `~donate`")
                .Do(async e =>
                {
                    await e.Channel.SendMessage(
$@"You can support the project on patreon. <https://patreon.com/nadekobot> or
You can send donations to `nadekodiscordbot@gmail.com`
Don't forget to leave your discord name or id in the message.

**Thank you** ♥️").ConfigureAwait(false);
                });
        }

        private static string PrintCommandHelp(Command com)
        {
            var str = "`" + com.Text + "`";
            str = com.Aliases.Aggregate(str, (current, a) => current + (", `" + a + "`"));
            str += " **Description:** " + com.Description + "\n";
            return str;
        }

        public HelpCommand(DiscordModule module) : base(module) { }
    }
}
