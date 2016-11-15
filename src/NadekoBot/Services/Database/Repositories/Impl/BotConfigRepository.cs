using NadekoBot.Services.Database.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class BotConfigRepository : Repository<BotConfig>, IBotConfigRepository
    {
        public BotConfigRepository(DbContext context) : base(context)
        {
        }

        public BotConfig GetOrCreate()
        {
            var config = _set.Include(bc => bc.RotatingStatusMessages)
                             .Include(bc => bc.RaceAnimals)
                             .Include(bc => bc.Blacklist)
                             .Include(bc => bc.EightBallResponses)
                             .Include(bc => bc.ModulePrefixes)
                             .FirstOrDefault();

            if (config == null)
            {
                _set.Add(config = new BotConfig());
                _context.SaveChanges();
            }
            return config;
        }
    }
}
