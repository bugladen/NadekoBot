using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace NadekoBot
{
    class HelpCommand : DiscordCommand
    {
        public override Func<CommandEventArgs, Task> DoFunc()
        {
            return async e =>
                {
                    string helpstr = "Official repo: **github.com/Kwoth/NadekoBot/** \n";

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
                        await client.SendMessage(e.User, curstr.Substring(0, curstr.LastIndexOf("\n")+1));
                        helpstr = curstr.Substring(curstr.LastIndexOf("\n")+1) + helpstr.Substring(2000);
                    }
                    await client.SendMessage(e.User, helpstr);
                };
        }

        public override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("-h")
                .Alias(new string[] { "-help", NadekoBot.botMention + " help", NadekoBot.botMention + " h" })
                .Description("Help command")
                .Do(DoFunc());
        }

        private string PrintCommandHelp(Command com)
        {
            var str = "`" + com.Text + "`";
            foreach (var a in com.Aliases)
                str += " [" + a + " ]";
            str += " **Description:** " + com.Description + "\n";
            return str;
        }
    }
}
