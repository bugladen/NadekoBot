using NadekoBot.Core.Modules.Gambling.Common;
using NadekoBot.Core.Services;
using System.Collections.Concurrent;

namespace NadekoBot.Modules.Gambling.Services
{
    public class GamblingService : INService
    {
        public ConcurrentDictionary<(ulong, ulong), RollDuelGame> Duels { get; } = new ConcurrentDictionary<(ulong, ulong), RollDuelGame>();
    }
}
