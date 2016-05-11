using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Modules;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.IO;
using System.Linq;
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
                if (com != null)
                    await e.Channel.SendMessage($"`Help for '{com.Text}':` {com.Description}").ConfigureAwait(false);
            }).ConfigureAwait(false);
        };
        public static string HelpString => (NadekoBot.IsBot
                                           ? $"To add me to your server, use this link** -> <https://discordapp.com/oauth2/authorize?client_id=170254782546575360&scope=bot&permissions=66186303>\n"
                                           : $"To invite me to your server, just send me an invite link here.") +
                                           $"You can use `{NadekoBot.Config.CommandPrefixes.Help}modules` command to see a list of all modules.\n" +
                                           $"You can use `{NadekoBot.Config.CommandPrefixes.Help}commands ModuleName`" +
                                           $" (for example `{NadekoBot.Config.CommandPrefixes.Help}commands Administration`) to see a list of all of the commands in that module.\n" +
                                           $"For a specific command help, use `{NadekoBot.Config.CommandPrefixes.Help}h \"Command name\"` (for example `-h \"!m q\"`)\n" +
                                           "**LIST OF COMMANDS CAN BE FOUND ON THIS LINK**\n\n <https://github.com/Kwoth/NadekoBot/blob/master/commandlist.md>";

        public static string DMHelpString => NadekoBot.Config.DMHelpString;

        public Action<CommandEventArgs> DoGitFunc() => e =>
        {
            string helpstr =
$@"######For more information and how to setup your own NadekoBot, go to: **http://github.com/Kwoth/NadekoBot/**
######You can donate on paypal: `nadekodiscordbot@gmail.com` or Bitcoin `17MZz1JAqME39akMLrVT4XBPffQJ2n1EPa`

#NadekoBot List Of Commands  
Version: `{NadekoStats.Instance.BotVersion}`";


            string lastCategory = "";
            foreach (var com in NadekoBot.Client.GetService<CommandService>().AllCommands)
            {
                if (com.Category != lastCategory)
                {
                    helpstr += "\n### " + com.Category + "  \n";
                    helpstr += "Command and aliases | Description | Usage\n";
                    helpstr += "----------------|--------------|-------\n";
                    lastCategory = com.Category;
                }
                helpstr += PrintCommandHelp(com);
            }
            helpstr = helpstr.Replace(NadekoBot.BotMention, "@BotName");
            helpstr = helpstr.Replace("\n**Usage**:", " | ").Replace("**Usage**:", " | ").Replace("**Description:**", " | ").Replace("\n|", " |  \n");
#if DEBUG
            File.WriteAllText("../../../commandlist.md", helpstr);
#else
            File.WriteAllText("commandlist.md", helpstr);
#endif
        };

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "h")
                .Alias(Module.Prefix + "help", NadekoBot.BotMention + " help", NadekoBot.BotMention + " h", "~h")
                .Description("Either shows a help for a single command, or PMs you help link if no arguments are specified.\n**Usage**: '-h !m q' or just '-h' ")
                .Parameter("command", ParameterType.Unparsed)
                .Do(HelpFunc());
            cgb.CreateCommand(Module.Prefix + "hgit")
                .Description("Generates the commandlist.md file. **Owner Only!**")
                .AddCheck(SimpleCheckers.OwnerOnly())
                .Do(DoGitFunc());
            cgb.CreateCommand(Module.Prefix + "readme")
                .Alias(Module.Prefix + "guide")
                .Description("Sends a readme and a guide links to the channel.")
                .Do(async e =>
                    await e.Channel.SendMessage(
@"**FULL README**: <https://github.com/Kwoth/NadekoBot/blob/master/README.md>

**WINDOWS SETUP GUIDE**: <https://github.com/Kwoth/NadekoBot/blob/master/ComprehensiveGuide.md>

**LINUX SETUP GUIDE**: <https://github.com/Kwoth/NadekoBot/blob/master/LinuxSetup.md>

**LIST OF COMMANDS**: <https://github.com/Kwoth/NadekoBot/blob/master/commandlist.md>").ConfigureAwait(false));

            cgb.CreateCommand(Module.Prefix + "donate")
                .Alias("~donate")
                .Description("Instructions for helping the project!")
                .Do(async e =>
                {
                    await e.Channel.SendMessage(
$@"I've created a **paypal** email for nadeko, so if you wish to support the project, you can send your donations to `nadekodiscordbot@gmail.com`
Don't forget to leave your discord name or id in the message, so that I can reward people who help out.
You can join nadekobot server by typing {Module.Prefix}h and you will get an invite in a private message.

*If you want to support in some other way or on a different platform, please message me*"
                    ).ConfigureAwait(false);
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
