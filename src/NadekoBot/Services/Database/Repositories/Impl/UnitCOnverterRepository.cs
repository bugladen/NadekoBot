using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class UnitConverterRepository : Repository<ConvertUnit>, IUnitConverterRepository
    {
        public UnitConverterRepository(DbContext context) : base(context)
        {
        }

        public void AddOrUpdate(Func<ConvertUnit, bool> check, ConvertUnit toAdd, Func<ConvertUnit, ConvertUnit> toUpdate)
        {
           var existing = _set.FirstOrDefault(check);
            if (existing != null)
            {
                existing = toUpdate.Invoke(existing);
            }
            else _set.Add(toAdd);
        }

        public bool Empty() => !_set.Any();
    }
}
