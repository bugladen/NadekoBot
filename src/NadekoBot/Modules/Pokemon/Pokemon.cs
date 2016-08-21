using Discord.Commands;
using Discord;
using NadekoBot.Attributes;
using System.Threading.Tasks;
using NadekoBot.Services;
using Discord.WebSocket;

namespace NadekoBot.Modules.Games
{
    [Module(">", AppendSpace = false)]
    public partial class Pokemon : DiscordModule
    {
        public Pokemon(ILocalization loc, CommandService cmds, IBotConfiguration config, DiscordSocketClient client) : base(loc, cmds, config, client)
        {
        }

        //todo Dragon should PR this in
        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Poke(IMessage imsg)
        {
            var channel = (ITextChannel)imsg.Channel;


        }
    }
}