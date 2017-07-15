using NadekoBot.DataStructures.ModuleBehaviors;
using NadekoBot.Services.Database.Models;
using System.Collections.Concurrent;
using System.Linq;
using Discord;
using System.Threading.Tasks;

namespace NadekoBot.Services.Permissions
{
    public class BlacklistService : IEarlyBlocker, INService
    {
        public ConcurrentHashSet<ulong> BlacklistedUsers { get; }
        public ConcurrentHashSet<ulong> BlacklistedGuilds { get; }
        public ConcurrentHashSet<ulong> BlacklistedChannels { get; }

        public BlacklistService(BotConfig bc)
        {
            var blacklist = bc.Blacklist;
            BlacklistedUsers = new ConcurrentHashSet<ulong>(blacklist.Where(bi => bi.Type == BlacklistType.User).Select(c => c.ItemId));
            BlacklistedGuilds = new ConcurrentHashSet<ulong>(blacklist.Where(bi => bi.Type == BlacklistType.Server).Select(c => c.ItemId));
            BlacklistedChannels = new ConcurrentHashSet<ulong>(blacklist.Where(bi => bi.Type == BlacklistType.Channel).Select(c => c.ItemId));
        }

        public Task<bool> TryBlockEarly(IGuild guild, IUserMessage usrMsg)
            => Task.FromResult((guild != null && BlacklistedGuilds.Contains(guild.Id)) ||
            BlacklistedChannels.Contains(usrMsg.Channel.Id) ||
            BlacklistedUsers.Contains(usrMsg.Author.Id));
    }
}
