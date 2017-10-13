using NadekoBot.Core.Services.Database.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Discord;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{
    public class DiscordUserRepository : Repository<DiscordUser>, IDiscordUserRepository
    {
        public DiscordUserRepository(DbContext context) : base(context)
        {
        }

        public DiscordUser GetOrCreate(IUser original)
        {
            DiscordUser toReturn;

            toReturn = _set.Include(x => x.Club)
                .FirstOrDefault(u => u.UserId == original.Id);

            if (toReturn != null)
            {
                toReturn.AvatarId = original.AvatarId;
                toReturn.Username = original.Username;
                toReturn.Discriminator = original.Discriminator;
            }

            if (toReturn == null)
                _set.Add(toReturn = new DiscordUser()
                {
                    AvatarId = original.AvatarId,
                    Discriminator = original.Discriminator,
                    UserId = original.Id,
                    Username = original.Username,
                    Club = null,
                });

            return toReturn;
        }

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
    }
}
