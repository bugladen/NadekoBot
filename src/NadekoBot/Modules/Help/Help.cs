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
using Discord.WebSocket;
using System.Collections;
using System.Collections.Generic;

namespace NadekoBot.Modules.Help
{
    [NadekoModule("Help", "-")]
    public partial class Help : DiscordModule
    {
        public string HelpString {
            get {
                var str = @"To add me to your server, use this link -> <https://discordapp.com/oauth2/authorize?client_id={0}&scope=bot&permissions=66186303>
You can use `{1}modules` command to see a list of all modules.
You can use `{1}commands ModuleName`
(for example `{1}commands Administration`) to see a list of all of the commands in that module.
For a specific command help, use `{1}h CommandName` (for example {1}h !!q)


**LIST OF COMMANDS CAN BE FOUND ON THIS LINK**
<https://github.com/Kwoth/NadekoBot/blob/master/commandlist.md>


Nadeko Support Server: https://discord.gg/0ehQwTK2RBjAxzEY";
                return String.Format(str, NadekoBot.Credentials.ClientId, NadekoBot.ModulePrefixes[typeof(Help).Name]);
            }
        }
        public Help(ILocalization loc, CommandService cmds, ShardedDiscordClient client) : base(loc, cmds, client)
        {
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        public async Task Modules(IUserMessage umsg)
        {

            await umsg.Channel.SendMessageAsync("`List of modules:` ```xl\n• " + string.Join("\n• ", _commands.Modules.Select(m => m.Name)) + $"\n``` `Type \"-commands module_name\" to get a list of commands in that module.`")
                                       .ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        public async Task Commands(IUserMessage umsg, [Remainder] string module = null)
        {
            var channel = umsg.Channel;

            module = module?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(module))
                return;
            var cmds = _commands.Commands.Where(c => c.Module.Name.ToUpperInvariant().StartsWith(module))
                                                  .OrderBy(c => c.Text)
                                                  .Distinct(new CommandTextEqualityComparer())
                                                  .AsEnumerable();

            var cmdsArray = cmds as Command[] ?? cmds.ToArray();
            if (!cmdsArray.Any())
            {
                await channel.SendMessageAsync("That module does not exist.").ConfigureAwait(false);
                return;
            }
            if (module != "customreactions" && module != "conversations")
            {
                await channel.SendTableAsync("`List Of Commands:`\n", cmdsArray, el => $"{el.Text,-15} {"["+el.Aliases.Skip(1).FirstOrDefault()+"]",-8}").ConfigureAwait(false);
            }
            else
            {
                await channel.SendMessageAsync("`List Of Commands:`\n• " + string.Join("\n• ", cmdsArray.Select(c => $"{c.Text}")));
            }
            await channel.SendMessageAsync($"`You can type \"{NadekoBot.ModulePrefixes[typeof(Help).Name]}h CommandName\" to see the help about that specific command.`").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        public async Task H(IUserMessage umsg, [Remainder] string comToFind = null)
        {
            var channel = umsg.Channel;

            comToFind = comToFind?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(comToFind))
            {
                await (await (umsg.Author as IGuildUser).CreateDMChannelAsync()).SendMessageAsync(HelpString).ConfigureAwait(false);
                return;
            }
            var com = _commands.Commands.FirstOrDefault(c => c.Text.ToLowerInvariant() == comToFind || c.Aliases.Select(a=>a.ToLowerInvariant()).Contains(comToFind));

            if (com == null)
            {
                await channel.SendMessageAsync("`No command found.`");
                return;
            }
            var str = $"**__Help for:__ `{com.Text}`**";
            var alias = com.Aliases.Skip(1).FirstOrDefault();
            if (alias != null)
                str += $" / `{ alias }`";
            if (com != null)
                await channel.SendMessageAsync(str + $@"{Environment.NewLine}**Desc:** {com.Remarks}
**Usage:** {com.Summary}").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public Task Hgit(IUserMessage umsg)
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
                helpstr.AppendLine($"`{com.Text}` {string.Join(" ", com.Aliases.Skip(1).Select(a=>"`"+a+"`"))} | {com.Remarks} | {com.Summary}");
            }
            helpstr = helpstr.Replace(NadekoBot.Client.GetCurrentUser().Username , "@BotName");
#if DEBUG
            File.WriteAllText("../../../../../docs/Commands List.md", helpstr.ToString());
#else
            File.WriteAllText("commandlist.md", helpstr.ToString());
#endif
            return Task.CompletedTask;
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task Guide(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            await channel.SendMessageAsync(
@"**LIST OF COMMANDS**: <http://nadekobot.readthedocs.io/en/latest/Commands%20List/>
**Hosting Guides and docs can be found here**: <http://nadekobot.rtfd.io>").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task Donate(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            await channel.SendMessageAsync(
$@"You can support the project on patreon. <https://patreon.com/nadekobot> or
You can send donations to `nadekodiscordbot@gmail.com`
Don't forget to leave your discord name or id in the message.

**Thank you** ♥️").ConfigureAwait(false);
        }
    }

    public class CommandTextEqualityComparer : IEqualityComparer<Command>
    {
        public bool Equals(Command x, Command y) => x.Text == y.Text;

        public int GetHashCode(Command obj) => obj.Text.GetHashCode();

    }
}