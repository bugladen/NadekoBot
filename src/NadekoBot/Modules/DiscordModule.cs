using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Services;
using NLog;

namespace NadekoBot.Modules
{
    public class DiscordModule
    {
        protected ILocalization _l { get; }
        protected CommandService _commands { get; }
        protected DiscordSocketClient _client { get; }
        protected Logger _log { get; }

        public DiscordModule(ILocalization loc, CommandService cmds, DiscordSocketClient client)
        {
            _l = loc;
            _commands = cmds;
            _client = client;
            _log = LogManager.GetCurrentClassLogger();
        }
    }
}
