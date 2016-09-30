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
        public Pokemon(ILocalization loc, CommandService cmds, ShardedDiscordClient client) : base(loc, cmds, client)
        {
        }

        //todo Dragon should PR this in
        [LocalizedCommand, LocalizedRemarks, LocalizedSummary, LocalizedAlias]
        [RequireContext(ContextType.Guild)]
        public async Task Poke(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;


        }
    }
}