using NadekoBot.Core.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{
    public class CurrencyRepository : Repository<Currency>, ICurrencyRepository
    {
        public CurrencyRepository(DbContext context) : base(context)
        {
        }

        public Currency GetOrCreate(ulong userId)
        {
            var cur = _set.FirstOrDefault(c => c.UserId == userId);

            if (cur == null)
            {
                _set.Add(cur = new Currency()
                {
                    UserId = userId,
                    Amount = 0
                });
                _context.SaveChanges();
            }
            return cur;
        }

        public IEnumerable<Currency> GetTopRichest(int count, int skip = 0) =>
            _set.OrderByDescending(c => c.Amount).Skip(skip).Take(count).ToList();

        public long GetUserCurrency(ulong userId) => 
            GetOrCreate(userId).Amount;

        public bool TryUpdateState(ulong userId, long change)
        {
            var cur = GetOrCreate(userId);

            if (change == 0)
                return true;

            if (change > 0)
            {
                cur.Amount += change;
                return true;
            }
            //change is negative
            if (cur.Amount + change >= 0)
            {
                cur.Amount += change;
                return true;
            }
            return false;
        }
    }
}
