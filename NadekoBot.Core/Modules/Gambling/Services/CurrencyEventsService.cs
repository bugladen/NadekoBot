using NadekoBot.Core.Services;
using NadekoBot.Core.Modules.Gambling.Common.Events;
using System.Collections.Concurrent;
using NadekoBot.Modules.Gambling.Common;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Gambling.Services
{
    public class CurrencyEventsService : INService
    {
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;
        private readonly ConcurrentDictionary<ulong, ICurrencyEvent> _events =
            new ConcurrentDictionary<ulong, ICurrencyEvent>();

        public CurrencyEventsService(DbService db, DiscordSocketClient client)
        {
            _db = db;
            _client = client;
        }

        public async Task<bool> TryCreateEvent(ulong guildId, ulong channelId, 
            ulong messageId, EventOptions opts)
        {
            SocketGuild g = _client.GetGuild(guildId);
            SocketTextChannel ch = g?.GetChannel(channelId) as SocketTextChannel;
            if (ch == null)
                return false;
            var msg = await ch.GetMessageAsync(messageId) as IUserMessage;
            if (msg == null)
                return false;

            if(opts.Type == Core.Services.Database.Models.Event.Type.Reaction)
            {
                ce = new ReactionEvent(_client, g, msg);
            }
            else //todo
            {
                ce = new ReactionEvent(_client, )
            }

            return _events.TryAdd(guildId, )
            return true;
        }
    }
}
