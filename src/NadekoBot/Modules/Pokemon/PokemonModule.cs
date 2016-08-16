using Discord.Commands;
using Discord;
using NadekoBot.Attributes;
using System.Threading.Tasks;
using NadekoBot.Services;

namespace NadekoBot.Modules.Games.Commands
{
    [Module(">", AppendSpace = false)]
    public partial class PokemonModule : DiscordModule
    {
        public PokemonModule(ILocalization loc, CommandService cmds, IBotConfiguration config, IDiscordClient client) : base(loc, cmds, config, client)
        {
        }

        //todo Dragon should PR this in
        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Poke(IMessage imsg)
        {
            var channel = imsg.Channel as IGuildChannel;


        }
    }
}