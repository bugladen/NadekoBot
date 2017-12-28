using System.Collections.Generic;
using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Core.Services.Database.Repositories
{
    public interface ICurrencyTransactionsRepository : IRepository<CurrencyTransaction>
    {
        List<CurrencyTransaction> GetPageFor(ulong userId, int page);
    }
}
