using NadekoBot.Services.Database.Models;
using System.Collections.Generic;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IClashOfClansRepository : IRepository<ClashWar>
    {
        IEnumerable<ClashWar> GetAllWars(List<long> guilds);
    }
}
