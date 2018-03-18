using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration.Common
{
    public class Ratelimiter
    {
        public ConcurrentDictionary<ulong, uint> Users { get; set; } = new ConcurrentDictionary<ulong, uint>();
        public uint MaxMessages { get; set; }
        public int PerSeconds { get; set; }
    }
}
