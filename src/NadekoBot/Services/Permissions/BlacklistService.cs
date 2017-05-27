using NadekoBot.Services.Database.Models;
using System.Collections.Concurrent;
using System.Linq;

namespace NadekoBot.Services.Permissions
{
    public class BlacklistService
    {
        public ConcurrentHashSet<ulong> BlacklistedUsers { get; set; }
        public ConcurrentHashSet<ulong> BlacklistedGuilds { get; set; }
        public ConcurrentHashSet<ulong> BlacklistedChannels { get; set; }

        public BlacklistService(BotConfig bc)
        {
            var blacklist = bc.Blacklist;
            BlacklistedUsers = new ConcurrentHashSet<ulong>(blacklist.Where(bi => bi.Type == BlacklistType.User).Select(c => c.ItemId));
            BlacklistedGuilds = new ConcurrentHashSet<ulong>(blacklist.Where(bi => bi.Type == BlacklistType.Server).Select(c => c.ItemId));
            BlacklistedChannels = new ConcurrentHashSet<ulong>(blacklist.Where(bi => bi.Type == BlacklistType.Channel).Select(c => c.ItemId));
        }
    }
}
