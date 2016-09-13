using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class GuildConfigRepository : Repository<GuildConfig>, IGuildConfigRepository
    {
        public GuildConfigRepository(DbContext context) : base(context)
        {
        }

        public new IEnumerable<GuildConfig> GetAll() =>
            _set.Include(gc => gc.LogSetting)
                    .ThenInclude(ls => ls.IgnoredChannels)
                .Include(gc => gc.LogSetting)
                    .ThenInclude(ls => ls.IgnoredVoicePresenceChannelIds)
                .ToList();

        /// <summary>
        /// Gets and creates if it doesn't exist a config for a guild.
        /// </summary>
        /// <param name="guildId"></param>
        /// <returns></returns>
        public GuildConfig For(ulong guildId)
        {
            var config = _set.Include(gc => gc.FollowedStreams)
                             .Include(gc => gc.LogSetting)
                                .ThenInclude(ls=>ls.IgnoredChannels)
                             .FirstOrDefault(c => c.GuildId == guildId);

            if (config == null)
            {
                _set.Add((config = new GuildConfig
                {
                    GuildId = guildId
                }));
                _context.SaveChanges();
            }
            return config;
        }

        public IEnumerable<FollowedStream> GetAllFollowedStreams() =>
            _set.Include(gc => gc.FollowedStreams)
                .SelectMany(gc => gc.FollowedStreams)
                .ToList();
    }
}
