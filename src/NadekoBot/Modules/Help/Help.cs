using Discord.Commands;
using NadekoBot.Extensions;
using System.Linq;
using Discord;
using NadekoBot.Services;
using System.Threading.Tasks;
using NadekoBot.Attributes;
using System;
using System.IO;
using System.Text;

namespace NadekoBot.Modules.Help
{
    [Module("-", AppendSpace = false)]
    public partial class Help : DiscordModule
    {
        public string HelpString {
            get {
                var str = "To add me to your server, use this link -> <https://discordapp.com/oauth2/authorize?client_id={0}&scope=bot&permissions=66186303>\n";
                return str + String.Format(str, NadekoBot.Credentials.ClientId);
            }
        }
        public Help(ILocalization loc, CommandService cmds, IBotConfiguration config, IDiscordClient client) : base(loc, cmds, config, client)
        {
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Modules(IMessage imsg)
        {
            var channel = imsg.Channel as ITextChannel;

            await imsg.Channel.SendMessageAsync("`List of modules:` \n• " + string.Join("\n• ", _commands.Modules.Select(m => m.Name)) + $"\n`Type \"-commands module_name\" to get a list of commands in that module.`")
                                       .ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Commands(IMessage imsg, [Remainder] string module = null)
        {
            var channel = imsg.Channel as ITextChannel;

            module = module?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(module))
                return;
            var cmds = _commands.Commands.Where(c => c.Module.Name.ToUpperInvariant() == module)
                                                  .OrderBy(c => c.Text)
                                                  .AsEnumerable();
            var cmdsArray = cmds as Command[] ?? cmds.ToArray();
            if (!cmdsArray.Any())
            {
                await imsg.Channel.SendMessageAsync("That module does not exist.").ConfigureAwait(false);
                return;
            }
            if (module != "customreactions" && module != "conversations")
            {
                //todo aliases
                await imsg.Channel.SendTableAsync("`List Of Commands:`\n", cmdsArray, el => $"{el.Text,-15}").ConfigureAwait(false);
            }
            else
            {
                await imsg.Channel.SendMessageAsync("`List Of Commands:`\n• " + string.Join("\n• ", cmdsArray.Select(c => $"{c.Text}")));
            }
            await imsg.Channel.SendMessageAsync($"`You can type \"-h command_name\" to see the help about that specific command.`").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task H(IMessage imsg, [Remainder] string comToFind = null)
        {
            var channel = imsg.Channel as ITextChannel;

            comToFind = comToFind?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(comToFind))
            {
                await (await (imsg.Author as IGuildUser).CreateDMChannelAsync()).SendMessageAsync(HelpString).ConfigureAwait(false);
                return;
            }
            var com = _commands.Commands.FirstOrDefault(c => c.Text.ToLowerInvariant() == comToFind);

            //todo aliases
            if (com != null)
                await imsg.Channel.SendMessageAsync($@"**__Help for:__ `{com.Text}`**
**Desc:** {com.Description}
**Usage:** {com.Summary}").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Hgit(IMessage imsg)
        {
            var helpstr = new StringBuilder();

            var lastModule = "";
            foreach (var com in _commands.Commands)
            {
                if (com.Module.Name != lastModule)
                {
                    helpstr.AppendLine("\n### " + com.Module.Name + "  ");
                    helpstr.AppendLine("Command and aliases | Description | Usage");
                    helpstr.AppendLine("----------------|--------------|-------");
                    lastModule = com.Module.Name;
                }
                //todo aliases
                helpstr.AppendLine($"`{com.Text}` | {com.Description} | {com.Summary}");
            }
            helpstr = helpstr.Replace((await NadekoBot.Client.GetCurrentUserAsync()).Username , "@BotName");
#if DEBUG
            File.WriteAllText("../../../../../docs/Commands List.md", helpstr.ToString());
#else
            File.WriteAllText("commandlist.md", helpstr.ToString());
#endif
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Guide(IMessage imsg)
        {
            var channel = imsg.Channel as ITextChannel;

            await imsg.Channel.SendMessageAsync(
@"**LIST OF COMMANDS**: <http://nadekobot.readthedocs.io/en/latest/Commands%20List/>
**Hosting Guides and docs can be found here**: <http://nadekobot.rtfd.io>").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Donate(IMessage imsg)
        {
            var channel = imsg.Channel as ITextChannel;

            await imsg.Channel.SendMessageAsync(
$@"You can support the project on patreon. <https://patreon.com/nadekobot> or
You can send donations to `nadekodiscordbot@gmail.com`
Don't forget to leave your discord name or id in the message.

**Thank you** ♥️").ConfigureAwait(false);
        }
    }
}