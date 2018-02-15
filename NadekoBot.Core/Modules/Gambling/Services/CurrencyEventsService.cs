using NadekoBot.Core.Services;
using NadekoBot.Core.Modules.Gambling.Common.Events;
using System.Collections.Concurrent;
using NadekoBot.Modules.Gambling.Common;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;
using NLog;
using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Modules.Gambling.Services
{
    public class CurrencyEventsService : INService
    {
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;
        private readonly ICurrencyService _cs;
        private readonly IBotConfigProvider _bc;
        private readonly Logger _log;
        private readonly ConcurrentDictionary<ulong, ICurrencyEvent> _events =
            new ConcurrentDictionary<ulong, ICurrencyEvent>();

        public CurrencyEventsService(DbService db, DiscordSocketClient client, ICurrencyService cs, IBotConfigProvider bc)
        {
            _db = db;
            _client = client;
            _cs = cs;
            _bc = bc;
            _log = LogManager.GetCurrentClassLogger();
        }

        public async Task<bool> TryCreateEventAsync(ulong guildId, ulong channelId, Event.Type type,
            EventOptions opts, Func<Event.Type, EventOptions, long, EmbedBuilder> embed)
        {
            SocketGuild g = _client.GetGuild(guildId);
            SocketTextChannel ch = g?.GetChannel(channelId) as SocketTextChannel;
            if (ch == null)
                return false;

            ICurrencyEvent ce;

            if (type == Event.Type.Reaction)
            {
                ce = new ReactionEvent(_client, _cs, _bc, g, ch, opts, embed);
            }
            else //todo
            {
                ce = new ReactionEvent(_client, _cs, _bc, g, ch, opts, embed);
            }

            var added = _events.TryAdd(guildId, ce);
            if (added)
            {
                try
                {
                    ce.OnEnded += OnEventEnded;
                    await ce.Start();
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    _events.TryRemove(guildId, out ce);
                    return false;
                }
            }
            return added;
        }

        private Task OnEventEnded(ulong gid)
        {
            _events.TryRemove(gid, out _);
            return Task.CompletedTask;
        }
    }
}
