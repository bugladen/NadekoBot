using NadekoBot.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Services;
using Discord;

namespace NadekoBot.Modules.Permissions
{
    [NadekoModule("Permissions", ";")]
    public class Permissions : DiscordModule
    {
        public Permissions(ILocalization loc, CommandService cmds, DiscordSocketClient client) : base(loc, cmds, client)
        {
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task UsrCmd(IUserMessage imsg, Command command, PermissionAction action, IGuildUser user)
        {
            var channel = (ITextChannel)imsg.Channel;

            await channel.SendMessageAsync($"{command.Text} {action.Value} {user}");
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task UsrMdl(IUserMessage imsg, Module module, PermissionAction action, IGuildUser user)
        {
            var channel = (ITextChannel)imsg.Channel;

            await channel.SendMessageAsync($"{module.Name} {action.Value} {user}");
        }
    }
}
