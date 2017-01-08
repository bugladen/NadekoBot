using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Discord
{
    public class ReactionEventWrapper : IDisposable
    {
        public SocketMessage Message { get; }
        public event Action<SocketReaction> OnReactionAdded = delegate { };
        public event Action<SocketReaction> OnReactionRemoved = delegate { };
        public event Action OnReactionsCleared = delegate { };

        public ReactionEventWrapper(SocketMessage msg)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));
            Message = msg;

            msg.Discord.ReactionAdded += Discord_ReactionAdded;
            msg.Discord.ReactionRemoved += Discord_ReactionRemoved;
            msg.Discord.ReactionsCleared += Discord_ReactionsCleared;
        }

        private Task Discord_ReactionsCleared(ulong messageId, Optional<SocketUserMessage> reaction)
        {
            if (messageId == Message.Id)
                OnReactionsCleared?.Invoke();
            return Task.CompletedTask;
        }

        private Task Discord_ReactionRemoved(ulong messageId, Optional<SocketUserMessage> arg2, SocketReaction reaction)
        {
            if (messageId == Message.Id)
                OnReactionRemoved?.Invoke(reaction);
            return Task.CompletedTask;
        }

        private Task Discord_ReactionAdded(ulong messageId, Optional<SocketUserMessage> message, SocketReaction reaction)
        {
            if(messageId == Message.Id)
                OnReactionAdded?.Invoke(reaction);
            return Task.CompletedTask;
        }

        public void UnsubAll()
        {
            Message.Discord.ReactionAdded -= Discord_ReactionAdded;
            Message.Discord.ReactionRemoved -= Discord_ReactionRemoved;
            Message.Discord.ReactionsCleared -= Discord_ReactionsCleared;
        }

        private bool disposing = false;
        public void Dispose()
        {
            if (disposing)
                return;
            disposing = true;
            UnsubAll();
        }
    }
}
