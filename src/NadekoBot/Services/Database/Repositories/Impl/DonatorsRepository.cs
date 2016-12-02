using System.Collections.Generic;
using System.Linq;
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
            var donator = _set.Where(d => d.UserId == userId).FirstOrDefault();

            if (donator == null)
            {
                _set.Add(donator = new Donator
                {
                    Amount = amount,
                    UserId = userId,
                    Name = name
                });
            }
            else
            {
                donator.Amount += amount;
                donator.Name = name;
                _set.Update(donator);
            }

            return donator;
        }

        public IEnumerable<Donator> GetDonatorsOrdered() => 
            _set.OrderByDescending(d => d.Amount).ToList();
    }
}
