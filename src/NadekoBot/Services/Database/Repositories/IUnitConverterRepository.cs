using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IUnitConverterRepository : IRepository<ConvertUnit>
    {
        void AddOrUpdate(Func<ConvertUnit, bool> check, ConvertUnit toAdd, Func<ConvertUnit, ConvertUnit> toUpdate);
        bool Empty();
    }
}
