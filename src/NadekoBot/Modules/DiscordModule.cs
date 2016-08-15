using Discord;
using Discord.Commands;
using NadekoBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules
{
    public class DiscordModule
    {
        protected ILocalization _l;
        protected CommandService _commands;
        protected IBotConfiguration _config;
        protected IDiscordClient _client;

        public DiscordModule(ILocalization loc, CommandService cmds, IBotConfiguration config,IDiscordClient client)
        {
            _l = loc;
            _commands = cmds;
            _config = config;
            _client = client;
        }
    }
}
