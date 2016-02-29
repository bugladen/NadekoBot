using System;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Extensions;
using System.IO;
using System.Linq;

namespace NadekoBot {
    class HelpCommand : DiscordCommand {
        public override Func<CommandEventArgs, Task> DoFunc() => async e => {
            #region OldHelp
            /*
            string helpstr = "**COMMANDS DO NOT WORK IN PERSONAL MESSAGES**\nOfficial repo: **github.com/Kwoth/NadekoBot/**";

            string lastCategory = "";
            foreach (var com in client.GetService<CommandService>().AllCommands)
            {
                if (com.Category != lastCategory)
                {
                    helpstr += "\n`----`**`" + com.Category + "`**`----`\n";
                    lastCategory = com.Category;
                }
                helpstr += PrintCommandHelp(com);
            }
            helpstr += "\nBot Creator's server: https://discord.gg/0ehQwTK2RBhxEi0X";
            helpstr = helpstr.Replace(NadekoBot.botMention, "@BotName");
            while (helpstr.Length > 2000)
            {
                var curstr = helpstr.Substring(0, 2000);
                await e.User.Send(curstr.Substring(0, curstr.LastIndexOf("\n") + 1));
                helpstr = curstr.Substring(curstr.LastIndexOf("\n") + 1) + helpstr.Substring(2000);
                await Task.Delay(200);
            }
            */
            #endregion OldHelp

            if (string.IsNullOrWhiteSpace(e.GetArg("command"))) {
                await e.User.Send("**LIST OF COMMANDS CAN BE FOUND ON THIS LINK**\n\n <https://github.com/Kwoth/NadekoBot/blob/master/commandlist.md>");
                return;
            }
            else {
                await Task.Run(async () => {
                    var comToFind = e.GetArg("command");

                    var com = NadekoBot.Client.GetService<CommandService>().AllCommands
                                            .Where(c => c.Text.ToLower().Equals(comToFind))
                                            .FirstOrDefault();
                    if (com != null)
                        await e.Channel.SendMessage($"`Help for '{com.Text}:'` **{com.Description}**");
                });
            }
        };

        public Action<CommandEventArgs> DoGitFunc() => e => {
            string helpstr =
$@"######For more information and how to setup your own NadekoBot, go to: **http://github.com/Kwoth/NadekoBot/**
######You can donate on paypal: `nadekodiscordbot@gmail.com` or Bitcoin `17MZz1JAqME39akMLrVT4XBPffQJ2n1EPa`

#NadekoBot List Of Commands  
Version: `{NadekoStats.Instance.BotVersion}`";


            string lastCategory = "";
            foreach (var com in client.GetService<CommandService>().AllCommands) {
                if (com.Category != lastCategory) {
                    helpstr += "\n### " + com.Category + "  \n";
                    helpstr += "Command and aliases | Description | Usage\n";
                    helpstr += "----------------|--------------|-------\n";
                    lastCategory = com.Category;
                }
                helpstr += PrintCommandHelp(com);
            }
            helpstr = helpstr.Replace(NadekoBot.botMention, "@BotName");
            helpstr = helpstr.Replace("\n**Usage**:", " | ").Replace("**Usage**:", " | ").Replace("**Description:**", " | ").Replace("\n|", " |  \n");
#if DEBUG
            File.WriteAllText("../../../commandlist.md", helpstr);
#else
            File.WriteAllText("commandlist.md", helpstr);
#endif
            return;
        };

        public override void Init(CommandGroupBuilder cgb) {
            cgb.CreateCommand("-h")
                .Alias(new string[] { "-help", NadekoBot.botMention + " help", NadekoBot.botMention + " h", "~h" })
                .Description("Either shows a help for a single command, or PMs you help link if no arguments are specified.\n**Usage**: '-h !m q' or just '-h' ")
                .Parameter("command", ParameterType.Unparsed)
                .Do(DoFunc());
            cgb.CreateCommand("-hgit")
                .Description("OWNER ONLY commandlist.md file generation.")
                .AddCheck(Classes.Permissions.SimpleCheckers.OwnerOnly())
                .Do(DoGitFunc());
            cgb.CreateCommand("-readme")
                .Alias("-guide")
                .Description("Sends a readme and a guide links to the channel.")
                .Do(async e =>
                    await e.Channel.SendMessage(
@"**FULL README**: <https://github.com/Kwoth/NadekoBot/blob/master/README.md>

**GUIDE ONLY**: <https://github.com/Kwoth/NadekoBot/blob/master/ComprehensiveGuide.md>

**LIST OF COMMANDS**: <https://github.com/Kwoth/NadekoBot/blob/master/commandlist.md>"));

            cgb.CreateCommand("-donate")
                .Alias("~donate")
                .Description("Instructions for helping the project!")
                .Do(async e => {
                    await e.Channel.SendMessage(
@"I've created a **paypal** email for nadeko, so if you wish to support the project, you can send your donations to `nadekodiscordbot@gmail.com`
Don't forget to leave your discord name or id in the message, so that I can reward people who help out.
You can join nadekobot server by simply private messaging NadekoBot, and you will get an invite.

*If you want to support in some other way or on a different platform, please message me there*"
                    );
                });
        }

        private string PrintCommandHelp(Command com) {
            var str = "`" + com.Text + "`";
            foreach (var a in com.Aliases)
                str += ", `" + a + "`";
            str += " **Description:** " + com.Description + "\n";
            return str;
        }
    }
}
