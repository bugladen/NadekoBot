using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Database.Repositories.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IConfigRepository : IRepository<Config>
    {
        Config For(ulong guildId);
    }
}
