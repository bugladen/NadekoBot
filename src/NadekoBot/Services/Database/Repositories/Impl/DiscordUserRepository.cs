using NadekoBot.Services.Database.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Discord;

namespace NadekoBot.Services.Database.Repositories.Impl
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
    }
}
