using NadekoBot.Core.Services.Database.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{
    public class XpRepository : Repository<UserXpStats>, IXpRepository
    {
        public XpRepository(DbContext context) : base(context)
        {
        }

        public UserXpStats GetOrCreateUser(ulong guildId, ulong userId)
        {
            var usr = _set.FirstOrDefault(x => x.UserId == userId && x.GuildId == guildId);

            if (usr == null)
            {
                _context.Add(usr = new UserXpStats()
                {
                    Xp = 0,
                    UserId = userId,
                    NotifyOnLevelUp = XpNotificationType.None,
                    GuildId = guildId,
                });
            }

            return usr;
        }

        public UserXpStats[] GetUsersFor(ulong guildId, int page)
        {
            return _set.Where(x => x.GuildId == guildId)
                .OrderByDescending(x => x.Xp + x.AwardedXp)
                .Skip(page * 9)
                .Take(9)
                .ToArray();
        }

        public int GetUserGuildRanking(ulong userId, ulong guildId)
        {
            if (!_set.Where(x => x.GuildId == guildId && x.UserId == userId).Any())
            {
                var cnt = _set.Count(x => x.GuildId == guildId);
                if (cnt == 0)
                    return 1;
                else
                    return cnt;
            }

            return _set
                .Where(x => x.GuildId == guildId)
                .Count(x => x.Xp > (_set
                    .Where(y => y.UserId == userId && y.GuildId == guildId)
                    .Select(y => y.Xp)
                    .DefaultIfEmpty()
                    .Sum())) + 1;
        }
    }
}