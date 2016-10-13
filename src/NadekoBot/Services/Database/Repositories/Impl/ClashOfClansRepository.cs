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
            return _set.Include(cw => cw.Bases)
                        .ToList();
        }
    }
}
