using NadekoBot.Core.Services.Database.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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
            //            @"SELECT COUNT(*) + 1
            //FROM UserXpStats
            //WHERE GuildId = @p1 AND ((Xp + AwardedXp) > (SELECT Xp + AwardedXp
            //	FROM UserXpStats
            //	WHERE UserId = @p2 AND GuildId = @p1
            //	LIMIT 1));";

            return _set
                .Where(x => x.GuildId == guildId && ((x.Xp + x.AwardedXp) >
                    (_set
                        .Where(y => y.UserId == userId && y.GuildId == guildId)
                        .Select(y => y.Xp + y.AwardedXp)
                        .FirstOrDefault())
                ))
                .Count() + 1;
        }

        public void ResetGuildUserXp(ulong userId, ulong guildId)
        {
            _context.Database.ExecuteSqlCommand($"DELETE FROM UserXpStats WHERE UserId={userId} AND GuildId={guildId};");
        }

        public void ResetGuildXp(ulong guildId)
        {
            _context.Database.ExecuteSqlCommand($"DELETE FROM UserXpStats WHERE GuildId={guildId};");
        }
    }
}