using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Modules.Utility.Common;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;

namespace NadekoBot.Modules.Utility.Services
{
    public class MessageRepeaterService : INService
    {
        private readonly DbService _db;

        //messagerepeater
        //guildid/RepeatRunners
        public ConcurrentDictionary<ulong, ConcurrentDictionary<int, RepeatRunner>> Repeaters { get; set; }
        public bool RepeaterReady { get; private set; }

        public MessageRepeaterService(NadekoBot bot, DiscordSocketClient client, DbService db)
        {
            _db = db;
            var _ = Task.Run(async () =>
            {
                await bot.Ready.Task.ConfigureAwait(false);

                Repeaters = new ConcurrentDictionary<ulong, ConcurrentDictionary<int, RepeatRunner>>(
                    bot.AllGuildConfigs
                        .Select(gc =>
                        {
                            var guild = client.GetGuild(gc.GuildId);
                            if (guild == null)
                                return (0, null);
                            return (gc.GuildId, new ConcurrentDictionary<int, RepeatRunner>(gc.GuildRepeaters
                                .Select(gr => new KeyValuePair<int, RepeatRunner>(gr.Id, new RepeatRunner(guild, gr, this)))
                                .Where(x => x.Value.Guild != null)));
                        })
                        .Where(x => x.Item2 != null)
                        .ToDictionary(x => x.GuildId, x => x.Item2));
                RepeaterReady = true;
            });
        }

        public async Task RemoveRepeater(Repeater r)
        {
            using (var uow = _db.UnitOfWork)
            {
                var gr = uow.GuildConfigs.ForId(r.GuildId, x => x.Include(y => y.GuildRepeaters)).GuildRepeaters;
                var toDelete = gr.FirstOrDefault(x => x.Id == r.Id);
                if (toDelete != null)
                    uow._context.Set<Repeater>().Remove(toDelete);
                await uow.CompleteAsync();
            }
        }

        public void SetRepeaterLastMessage(int repeaterId, ulong lastMsgId)
        {
            using (var uow = _db.UnitOfWork)
            {
                uow._context.Database.ExecuteSqlCommand($@"UPDATE GuildRepeater SET 
                    LastMessageId={lastMsgId} WHERE Id={repeaterId}");
            }
        }
    }
}
