using NadekoBot.Services.Database.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Services.Permissions
{
    public class CmdCdService
    {
        public ConcurrentDictionary<ulong, ConcurrentHashSet<CommandCooldown>> CommandCooldowns { get; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<ActiveCooldown>> ActiveCooldowns { get; } = new ConcurrentDictionary<ulong, ConcurrentHashSet<ActiveCooldown>>();

        public CmdCdService(IEnumerable<GuildConfig> gcs)
        {
            CommandCooldowns = new ConcurrentDictionary<ulong, ConcurrentHashSet<CommandCooldown>>(
                gcs.ToDictionary(k => k.GuildId, 
                                 v => new ConcurrentHashSet<CommandCooldown>(v.CommandCooldowns)));
        }
    }

    public class ActiveCooldown
    {
        public string Command { get; set; }
        public ulong UserId { get; set; }
    }
}
