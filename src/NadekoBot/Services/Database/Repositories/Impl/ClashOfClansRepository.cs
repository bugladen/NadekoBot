using NadekoBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class ClashOfClansRepository : Repository<ClashWar>, IClashOfClansRepository
    {
        public ClashOfClansRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<ClashWar> GetAllWars(List<long> guilds)
        {
            var toReturn =  _set
                .Where(cw => guilds.Contains((long)cw.GuildId))
                .Include(cw => cw.Bases)
                        .ToList();
            toReturn.ForEach(cw => cw.Bases = cw.Bases.Where(w => w.SequenceNumber != null).OrderBy(w => w.SequenceNumber).ToList());
            return toReturn;
        }
    }
}
