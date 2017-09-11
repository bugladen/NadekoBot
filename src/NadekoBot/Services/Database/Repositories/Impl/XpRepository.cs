using NadekoBot.Services.Database.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

//todo add pagination to .lb
namespace NadekoBot.Services.Database.Repositories.Impl
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

        public int GetTotalUserXp(ulong userId)
        {
            return _set.Where(x => x.UserId == userId).Sum(x => x.Xp);
        }

        public UserXpStats[] GetUsersFor(ulong guildId, int page)
        {
            return _set.Where(x => x.GuildId == guildId)
                .OrderByDescending(x => x.Xp + x.AwardedXp)
                .Skip(page * 9)
                .Take(9)
                .ToArray();
        }

        public int GetUserGlobalRanking(ulong userId)
        {
            return _set
                .GroupBy(x => x.UserId)
                .Count(x => x.Sum(y => y.Xp) > _set
                    .Where(y => y.UserId == userId)
                    .Sum(y => y.Xp)) + 1;
        }

        public int GetUserGuildRanking(ulong userId, ulong guildId)
        {
            return _set
                .Where(x => x.GuildId == guildId)
                .Count(x => x.Xp > (_set
                    .Where(y => y.UserId == userId && y.GuildId == guildId)
                    .Select(y => y.Xp)
                    .DefaultIfEmpty()
                    .Sum())) + 1;
        }

        public (ulong UserId, int TotalXp)[] GetUsersFor(int page)
        {
            return (from orduser in _set
                    group orduser by orduser.UserId into g
                    orderby g.Sum(x => x.Xp) descending
                    select new { UserId = g.Key, TotalXp = g.Sum(x => x.Xp) })
                    .Skip(page * 9)
                    .Take(9)
                    .AsEnumerable()
                    .Select(x => (x.UserId, x.TotalXp))
                    .ToArray();
        }
    }
}