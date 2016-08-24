using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IDonatorsRepository : IRepository<Donator>
    {
        IEnumerable<Donator> GetDonatorsOrdered();
        Donator AddOrUpdateDonator(ulong userId, string name, int amount);
    }
}
