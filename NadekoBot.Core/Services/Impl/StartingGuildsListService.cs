using Discord.WebSocket;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Collections;

namespace NadekoBot.Core.Services.Impl
{
    public class StartingGuildsService : IEnumerable<long>, INService
    {
        private readonly ImmutableList<long> _guilds;

        public StartingGuildsService(DiscordSocketClient client)
        {
            this._guilds = client.Guilds.Select(x => (long)x.Id).ToImmutableList();
        }

        public IEnumerator<long> GetEnumerator() =>
            _guilds.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            _guilds.GetEnumerator();
    }
}
