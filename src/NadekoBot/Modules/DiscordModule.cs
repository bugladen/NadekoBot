using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Services;
using NLog;

namespace NadekoBot.Modules
{
    public class DiscordModule
    {
        protected ILocalization _l;
        protected CommandService _commands;
        protected IBotConfiguration _config;
        protected DiscordSocketClient _client;
        protected Logger _log;

        public DiscordModule(ILocalization loc, CommandService cmds, IBotConfiguration config, DiscordSocketClient client)
        {
            _l = loc;
            _commands = cmds;
            _config = config;
            _client = client;
            _log = LogManager.GetCurrentClassLogger();
        }
    }
}
