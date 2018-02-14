using Discord;
using Discord.WebSocket;
using NadekoBot.Modules.Gambling.Common;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Core.Modules.Gambling.Common.Events
{
    public class ReactionEvent : ICurrencyEvent
    {
        private readonly DiscordSocketClient _client;
        private readonly IGuild _guild;
        private readonly IUserMessage _msg;

        public ReactionEvent(DiscordSocketClient client, IGuild guild, IUserMessage msg)
        {
            _client = client;
            _guild = guild ?? throw new ArgumentNullException(nameof(guild));
            _msg = msg ?? throw new ArgumentNullException(nameof(msg));

        }

        public Task Start()
        {
            _client.ReactionAdded += HandleReaction;
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            throw new NotImplementedException();
        }
        
        private Task HandleReaction(Cacheable<IUserMessage, ulong> msg, 
            ISocketMessageChannel ch, SocketReaction r)
        {
            var _ = Task.Run(() =>
            {
                if (msg.Id != _msg.Id)
                    return;



            });
            return Task.CompletedTask;
        }
    }
}
