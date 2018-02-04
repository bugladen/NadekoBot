using System.Linq;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Common.Collections;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Modules.Permissions.Services
{
    public class BlacklistService : IEarlyBlocker, INService
    {
        public ConcurrentHashSet<ulong> BlacklistedUsers { get; }
        public ConcurrentHashSet<ulong> BlacklistedGuilds { get; }
        public ConcurrentHashSet<ulong> BlacklistedChannels { get; }

        public BlacklistService(IBotConfigProvider bc)
        {
            var blacklist = bc.BotConfig.Blacklist;
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
