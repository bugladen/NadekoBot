using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class ClashOfClansRepository : Repository<ClashWar>, IClashOfClansRepository
    {
        public ClashOfClansRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<ClashWar> GetAllWars()
        {
            var toReturn =  _set.Include(cw => cw.Bases)
                        .ToList();
            toReturn.ForEach(cw => cw.Bases = cw.Bases.Where(w => w.SequenceNumber != null).OrderBy(w => w.SequenceNumber).ToList());
            return toReturn;
        }
    }
}
