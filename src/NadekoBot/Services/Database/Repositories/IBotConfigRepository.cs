using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;
using System;
using System.Linq;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IBotConfigRepository : IRepository<BotConfig>
    {
        BotConfig GetOrCreate(Func<DbSet<BotConfig>, IQueryable<BotConfig>> includes = null);
    }
}
