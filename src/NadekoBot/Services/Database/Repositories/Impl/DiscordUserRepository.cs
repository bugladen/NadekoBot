using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            toReturn = _set.FirstOrDefault(u => u.UserId == original.Id);

            if (toReturn == null)
                _set.Add(toReturn = new DiscordUser()
                {
                    AvatarId = original.AvatarId,
                    Discriminator = original.Discriminator,
                    UserId = original.Id,
                    Username = original.Username,
                });

            return toReturn;
        }
    }
}
