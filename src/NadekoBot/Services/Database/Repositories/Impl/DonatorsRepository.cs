using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class DonatorsRepository : Repository<Donator>, IDonatorsRepository
    {
        public DonatorsRepository(DbContext context) : base(context)
        {
        }

        public Donator AddOrUpdateDonator(ulong userId, string name, int amount)
        {
            var donator = _set.Find(userId);

            if (donator == null)
            {
                _set.Add(donator = new Donator
                {
                    Amount = amount,
                    UserId = userId
                });
            }
            else
            {
                donator.Amount += amount;
                _set.Update(donator);
            }

            return donator;
        }

        public IEnumerable<Donator> GetDonatorsOrdered() => 
            _set.OrderByDescending(d => d.Amount).ToList();
    }
}
