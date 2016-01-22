using System;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Extensions;
using System.IO;

namespace NadekoBot
{
    class HelpCommand : DiscordCommand
    {
        public override Func<CommandEventArgs, Task> DoFunc() => async e =>
        {
            string helpstr = "Official repo: **github.com/Kwoth/NadekoBot/**\nBot Creator's server: https://discord.gg/0ehQwTK2RBhxEi0X";

            string lastCategory = "";
            foreach (var com in client.Commands().AllCommands)
            {
                if (com.Category != lastCategory)
                {
                    helpstr += "\n`----`**`" + com.Category + "`**`----`\n";
                    lastCategory = com.Category;
                }
                helpstr += PrintCommandHelp(com);
            }
            helpstr = helpstr.Replace(NadekoBot.botMention, "@BotName");
            while (helpstr.Length > 2000)
            {
                var curstr = helpstr.Substring(0, 2000);
                await e.User.Send(curstr.Substring(0, curstr.LastIndexOf("\n") + 1));
                helpstr = curstr.Substring(curstr.LastIndexOf("\n") + 1) + helpstr.Substring(2000);
            }
            await e.User.Send(helpstr);
        };

        public Action<CommandEventArgs> DoGitFunc() => e => {
            string helpstr = "Official repo: **github.com/Kwoth/NadekoBot/** \n";

            string lastCategory = "";
            foreach (var com in client.Commands().AllCommands) {
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
            File.WriteAllText("readme.md",helpstr);
            return;
        };

        public override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("-h")
                .Alias(new string[] { "-help", NadekoBot.botMention + " help", NadekoBot.botMention + " h" })
                .Description("Help command")
                .Do(DoFunc());
            cgb.CreateCommand("-hgit")
                .Description("Help command stylized for github readme")
                .Do(DoGitFunc());
        }

        private string PrintCommandHelp(Command com)
        {
            var str = "`" + com.Text + "`";
            foreach (var a in com.Aliases)
                str += ", `" + a + "`";
            str += " **Description:** " + com.Description + "\n";
            return str;
        }
    }
}
