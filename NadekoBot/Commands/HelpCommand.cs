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
                    string helpstr = "Official repo: https://github.com/Kwoth/NadekoBot/"+Environment.NewLine;
                    foreach (var com in client.Commands().AllCommands)
                    {
                        helpstr += "&###**#" + com.Category + "#**\n";
                        helpstr += PrintCommandHelp(com);
                    }
                    while (helpstr.Length > 2000)
                    {
                        var curstr = helpstr.Substring(0, 2000);
                        await client.SendPrivateMessage(e.User, curstr.Substring(0, curstr.LastIndexOf("&")));
                        helpstr = curstr.Substring(curstr.LastIndexOf("&")) + helpstr.Substring(2000);
                    }
                    await client.SendPrivateMessage(e.User, helpstr);
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
            var str = "`" + com.Text + "`\n";
            foreach (var a in com.Aliases)
                str += "`" + a + "`\n";
            str += "Description: " + com.Description + "\n";
            return str;
        }
    }
}
