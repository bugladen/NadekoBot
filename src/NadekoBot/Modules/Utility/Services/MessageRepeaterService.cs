using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Modules.Utility.Common;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Utility.Services
{
    //todo 50 rewrite
    public class MessageRepeaterService : INService
    {
        //messagerepeater
        //guildid/RepeatRunners
        public ConcurrentDictionary<ulong, ConcurrentQueue<RepeatRunner>> Repeaters { get; set; }
        public bool RepeaterReady { get; private set; }

        public MessageRepeaterService(NadekoBot bot, DiscordSocketClient client, IEnumerable<GuildConfig> gcs)
        {
            var _ = Task.Run(async () =>
            {
                await bot.Ready.Task.ConfigureAwait(false);

                Repeaters = new ConcurrentDictionary<ulong, ConcurrentQueue<RepeatRunner>>(gcs
                    .ToDictionary(gc => gc.GuildId,
                        gc => new ConcurrentQueue<RepeatRunner>(gc.GuildRepeaters
                            .Select(gr => new RepeatRunner(client, gr))
                            .Where(x => x.Guild != null))));
                RepeaterReady = true;
            });
        }
    }
}
