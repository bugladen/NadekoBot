using NadekoBot.Core.Services.Database.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Discord;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{
    public class DiscordUserRepository : Repository<DiscordUser>, IDiscordUserRepository
    {
        public DiscordUserRepository(DbContext context) : base(context)
        {
        }

        //temp is only used in updatecurrencystate, so that i don't overwrite real usernames/discrims with Unknown
        public DiscordUser GetOrCreate(ulong userId, string username, string discrim, string avatarId)
        {
            _context.Database.ExecuteSqlCommand($@"
UPDATE OR IGNORE DiscordUser 
SET Username={username},
    Discriminator={discrim},
    AvatarId={avatarId}
WHERE UserId={userId};

INSERT OR IGNORE INTO DiscordUser (UserId, Username, Discriminator, AvatarId)
VALUES ({userId}, {username}, {discrim}, {avatarId});
");
            return _set
                .Include(x => x.Club)
                .First(u => u.UserId == userId);
        }

        public DiscordUser GetOrCreate(IUser original)
            => GetOrCreate(original.Id, original.Username, original.Discriminator, original.AvatarId);

        public async Task<int> GetUserGlobalRankingAsync(ulong id)
        {
            if (!_set.Where(y => y.UserId == id).Any())
            {
                return await _set.CountAsync() + 1;
            }
            return await _set.CountAsync(x => x.TotalXp >=
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

        public IEnumerable<DiscordUser> GetTopRichest(ulong botId, int count, int skip = 0)
        {
            return _set.Where(c => c.CurrencyAmount > 0 && botId != c.UserId)
                .OrderByDescending(c => c.CurrencyAmount)
                .Skip(skip)
                .Take(count)
                .ToList();
        }

        public long GetUserCurrency(ulong userId) =>
            _set.FirstOrDefault(x => x.UserId == userId)?.CurrencyAmount ?? 0;

        public long GetUserCurrency(IUser user) =>
            GetOrCreate(user).CurrencyAmount;

        public void RemoveFromMany(List<long> ids)
        {
            var items = _set.Where(x => ids.Contains((long)x.UserId));
            foreach (var item in items)
            {
                item.CurrencyAmount = 0;
            }
        }

        public bool TryUpdateCurrencyState(ulong userId, string name, string discrim, string avatarId, long amount, bool allowNegative = false)
        {
            if (amount == 0)
                return true;

            // if remove - try to remove if he has more or equal than the amount
            // and return number of rows > 0 (was there a change)
            if (amount < 0 && !allowNegative)
            {
                var rows = _context.Database.ExecuteSqlCommand($@"
UPDATE DiscordUser
SET CurrencyAmount=CurrencyAmount+{amount}
WHERE UserId={userId} AND CurrencyAmount>={-amount}");
                return rows > 0;
            }

            // if remove and negative is allowed, just remove without any condition
            if (amount < 0 && allowNegative)
            {
                var rows = _context.Database.ExecuteSqlCommand($@"
UPDATE DiscordUser
SET CurrencyAmount=CurrencyAmount+{amount}
WHERE UserId={userId}");
                return rows > 0;
            }

            // if add - create a new user with default values if it doesn't exist
            // if it exists, sum current amount with the new one, if it doesn't
            // he just has the new amount
            var updatedUserData = !string.IsNullOrWhiteSpace(name);
            name = name ?? "Unknown";
            discrim = discrim ?? "????";
            avatarId = avatarId ?? "";

            // just update the amount, there is no new user data
            if (!updatedUserData)
            {
                _context.Database.ExecuteSqlCommand($@"
UPDATE OR IGNORE DiscordUser 
SET CurrencyAmount=CurrencyAmount+{amount}
WHERE UserId={userId};

INSERT OR IGNORE INTO DiscordUser (UserId, Username, Discriminator, AvatarId, CurrencyAmount)
VALUES ({userId}, {name}, {discrim}, {avatarId}, {amount});
");
            }
            else
            {
                _context.Database.ExecuteSqlCommand($@"
UPDATE OR IGNORE DiscordUser 
SET CurrencyAmount=CurrencyAmount+{amount},
    Username={name},
    Discriminator={discrim},
    AvatarId={avatarId}
WHERE UserId={userId};

INSERT OR IGNORE INTO DiscordUser (UserId, Username, Discriminator, AvatarId, CurrencyAmount)
VALUES ({userId}, {name}, {discrim}, {avatarId}, {amount});
");
            }
            return true;
        }
    }
}
