using NadekoBot.Core.Services.Database.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Discord;
using System.Collections.Generic;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{
    public class DiscordUserRepository : Repository<DiscordUser>, IDiscordUserRepository
    {
        public DiscordUserRepository(DbContext context) : base(context)
        {
        }

        //temp is only used in updatecurrencystate, so that i don't overwrite real usernames/discrims with Unknown
        public DiscordUser GetOrCreate(ulong userId, string username, string discrim, string avatarId, bool temp = false)
        {
            DiscordUser toReturn;

            toReturn = _set.Include(x => x.Club)
                .FirstOrDefault(u => u.UserId == userId);

            if (toReturn != null && !temp)
            {
                toReturn.AvatarId = avatarId;
                toReturn.Username = username;
                toReturn.Discriminator = discrim;
            }

            if (toReturn == null)
                _set.Add(toReturn = new DiscordUser()
                {
                    AvatarId = avatarId,
                    Discriminator = discrim,
                    UserId = userId,
                    Username = username,
                    Club = null,
                });

            return toReturn;
        }

        public DiscordUser GetOrCreate(IUser original)
            => GetOrCreate(original.Id, original.Username, original.Discriminator, original.AvatarId);

        public int GetUserGlobalRanking(ulong id)
        {
            if (!_set.Where(y => y.UserId == id).Any())
            {
                return _set.Count() + 1;
            }
            return _set.Count(x => x.TotalXp >= 
                _set.Where(y => y.UserId == id)
                    .DefaultIfEmpty()
                    .Sum(y => y.TotalXp));
        }

        public DiscordUser[] GetUsersXpLeaderboardFor(int page)
        {
            return _set
                .OrderByDescending(x => x.TotalXp)
                .Skip(page * 9)
                .Take(9)
                .AsEnumerable()
                .ToArray();
        }

        public IEnumerable<DiscordUser> GetTopRichest(int count, int skip = 0) =>
            _set.Where(c => c.CurrencyAmount > 0).OrderByDescending(c => c.CurrencyAmount).Skip(skip).Take(count).ToList();

        public long GetUserCurrency(ulong userId) =>
            _set.FirstOrDefault(x => x.UserId == userId)?.CurrencyAmount ?? 0;

        public long GetUserCurrency(IUser user) =>
            GetOrCreate(user).CurrencyAmount;

        public void RemoveFromMany(List<long> ids)
        {
            _set.RemoveRange(_set.Where(x => ids.Contains((long)x.UserId)));
        }

        public bool TryUpdateCurrencyState(ulong userId, long change, bool allowNegative = false)
        {
            var cur = GetOrCreate(userId, "Unknown", "????", "", true);

            if (change == 0)
                return true;

            if (change > 0)
            {
                cur.CurrencyAmount += change;
                return true;
            }
            //change is negative
            if (cur.CurrencyAmount + change < 0)
            {
                if (allowNegative)
                    cur.CurrencyAmount += change;
                return false;
            }
            cur.CurrencyAmount += change;
            return true;
        }
    }
}
