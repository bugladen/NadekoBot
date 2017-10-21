using NadekoBot.Core.Services;
using System;
using System.Collections.Concurrent;

namespace NadekoBot.Modules.Gambling.Services
{
    public class WaifuService : INService
    {
        public ConcurrentDictionary<ulong, DateTime> DivorceCooldowns { get; } = new ConcurrentDictionary<ulong, DateTime>();
        public ConcurrentDictionary<ulong, DateTime> AffinityCooldowns { get; } = new ConcurrentDictionary<ulong, DateTime>();

        
    }
}
