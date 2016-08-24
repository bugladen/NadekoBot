using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class ConfigRepository : Repository<Config>, IConfigRepository
    {
        public ConfigRepository(DbContext context) : base(context)
        {
        }
        /// <summary>
        /// Gets and creates if it doesn't exist a config for a guild.
        /// </summary>
        /// <param name="guildId"></param>
        /// <returns></returns>
        public Config For(ulong guildId)
        {
            var config = _set.Where(c => c.GuildId == guildId).FirstOrDefault();

            if (config == null)
            {
                _set.Add((config = new Config
                {
                    GuildId = guildId
                }));
            }
            return config;
        }
    }
}
