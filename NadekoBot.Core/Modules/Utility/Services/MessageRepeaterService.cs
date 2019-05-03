using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Utility.Common;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility.Services
{
    public class MessageRepeaterService : INService
    {
        private readonly DbService _db;
        private readonly Logger _log;
        private readonly NadekoBot _bot;
        private readonly DiscordSocketClient _client;

        public ConcurrentDictionary<ulong, ConcurrentDictionary<int, RepeatRunner>> Repeaters { get; set; }
        public bool RepeaterReady { get; private set; }

        public MessageRepeaterService(NadekoBot bot, DiscordSocketClient client, DbService db)
        {
            _db = db;
            _log = LogManager.GetCurrentClassLogger();
            _bot = bot;
            _client = client;
            var _ = LoadRepeaters();
        }

        private async Task LoadRepeaters()
        {
            await _bot.Ready.Task.ConfigureAwait(false);
#if GLOBAL_NADEKO
            await Task.Delay(30000);
#endif
            _log.Info("Loading message repeaters on shard {ShardId}.", _client.ShardId);

            var repeaters = new Dictionary<ulong, ConcurrentDictionary<int, RepeatRunner>>();
            var toDelete = new List<Repeater>();
            foreach (var gc in _bot.AllGuildConfigs)
            {
                try
                {
                    var guild = _client.GetGuild(gc.GuildId);
                    if (guild is null)
                    {
                        _log.Info("Unable to find guild {GuildId} for message repeaters. Removing.", gc.GuildId);
                        toDelete.AddRange(gc.GuildRepeaters);
                        continue;
                    }

                    var idToRepeater = gc.GuildRepeaters
                        .Select(gr => new KeyValuePair<int, RepeatRunner>(gr.Id, new RepeatRunner(guild, gr, this)))
                        .ToDictionary(x => x.Key, y => y.Value)
                        .ToConcurrent();


                    repeaters.TryAdd(gc.GuildId, idToRepeater);
                }
                catch (Exception ex)
                {
                    _log.Error("Failed to load repeaters on Guild {0}.", gc.GuildId);
                    _log.Error(ex);
                }
            }

            Repeaters = repeaters.ToConcurrent();
            RepeaterReady = true;
        }

        public async Task RemoveRepeater(Repeater r)
        {
            using (var uow = _db.GetDbContext())
            {
                var gr = uow.GuildConfigs.ForId(r.GuildId, x => x.Include(y => y.GuildRepeaters)).GuildRepeaters;
                var toDelete = gr.FirstOrDefault(x => x.Id == r.Id);
                if (toDelete != null)
                    uow._context.Set<Repeater>().Remove(toDelete);
                await uow.SaveChangesAsync();
            }
        }

        public void SetRepeaterLastMessage(int repeaterId, ulong lastMsgId)
        {
            using (var uow = _db.GetDbContext())
            {
                uow._context.Database.ExecuteSqlCommand($@"UPDATE GuildRepeater SET 
                    LastMessageId={lastMsgId} WHERE Id={repeaterId}");
            }
        }
    }
}
