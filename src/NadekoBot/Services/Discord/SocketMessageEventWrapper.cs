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
        public IUserMessage Message { get; }
        public event Action<SocketReaction> OnReactionAdded = delegate { };
        public event Action<SocketReaction> OnReactionRemoved = delegate { };
        public event Action OnReactionsCleared = delegate { };

        public ReactionEventWrapper(IUserMessage msg)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));
            Message = msg;

            NadekoBot.Client.ReactionAdded += Discord_ReactionAdded;
            NadekoBot.Client.ReactionRemoved += Discord_ReactionRemoved;
            NadekoBot.Client.ReactionsCleared += Discord_ReactionsCleared;
        }

        private Task Discord_ReactionsCleared(ulong messageId, Optional<SocketUserMessage> reaction)
        {
            try
            {
                if (messageId == Message.Id)
                    OnReactionsCleared?.Invoke();
            }
            catch { }

            return Task.CompletedTask;
        }

        private Task Discord_ReactionRemoved(ulong messageId, Optional<SocketUserMessage> arg2, SocketReaction reaction)
        {
            try
            {
                if (messageId == Message.Id)
                    OnReactionRemoved?.Invoke(reaction);
            }
            catch { }

            return Task.CompletedTask;
        }

        private Task Discord_ReactionAdded(ulong messageId, Optional<SocketUserMessage> message, SocketReaction reaction)
        {
            try
            {
                if (messageId == Message.Id)
                    OnReactionAdded?.Invoke(reaction);
            }
            catch { }

            return Task.CompletedTask;
        }

        public void UnsubAll()
        {
            NadekoBot.Client.ReactionAdded -= Discord_ReactionAdded;
            NadekoBot.Client.ReactionRemoved -= Discord_ReactionRemoved;
            NadekoBot.Client.ReactionsCleared -= Discord_ReactionsCleared;
            OnReactionAdded = null;
            OnReactionRemoved = null;
            OnReactionsCleared = null;
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
