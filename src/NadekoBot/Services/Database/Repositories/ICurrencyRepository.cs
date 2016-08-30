using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Repositories
{
    public interface ICurrencyRepository : IRepository<Currency>
    {
        Currency GetOrCreate(ulong userId);
        long GetUserCurrency(ulong userId);
        bool TryUpdateState(ulong userId, long change);
        IEnumerable<Currency> GetTopRichest(int count);
    }
}
