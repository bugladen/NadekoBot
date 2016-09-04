using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration {
        [Group]
        public class LogCommands
        {
            private DiscordSocketClient _client;

            private string prettyCurrentTime => $"【{DateTime.Now:HH:mm:ss}】";

            public LogCommands(DiscordSocketClient client)
            {
                _client = client;
                _client.MessageReceived += _client_MessageReceived;
            }

            private Task _client_MessageReceived(IMessage arg)
            {
                throw new NotImplementedException();
            }
        }
    }
}