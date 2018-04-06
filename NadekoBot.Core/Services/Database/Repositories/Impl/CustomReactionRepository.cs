using NadekoBot.Core.Services.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{
    public class CustomReactionsRepository : Repository<CustomReaction>, ICustomReactionRepository
    {
        public CustomReactionsRepository(DbContext context) : base(context)
        {
        }

        public CustomReaction[] For(ulong id)
        {
            return _set.Where(x => x.GuildId == id)
                .ToArray();
        }

        /// <summary>
        /// Gets all global custom reactions and custom reactions only for the specified guild ids
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public CustomReaction[] GetGlobalAndFor(IEnumerable<long> ids)
        {
            return _set.Where(x => x.GuildId == null || x.GuildId == 0 || ids.Contains((long)x.GuildId))
                .ToArray();
        }
    }
}
