using NadekoBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class GuildConfigRepository : Repository<GuildConfig>, IGuildConfigRepository
    {
        public GuildConfigRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<GuildConfig> GetAllGuildConfigs() =>
            _set.Include(gc => gc.LogSetting)
                    .ThenInclude(ls => ls.IgnoredChannels)
                .Include(gc => gc.MutedUsers)
                .Include(gc => gc.UnmuteTimers)
                .Include(gc => gc.VcRoleInfos)
                .Include(gc => gc.GenerateCurrencyChannelIds)
                .Include(gc => gc.FilterInvitesChannelIds)
                .Include(gc => gc.FilterWordsChannelIds)
                .Include(gc => gc.FilteredWords)
                .Include(gc => gc.CommandCooldowns)
                .Include(gc => gc.GuildRepeaters)
                .Include(gc => gc.AntiRaidSetting)
                .Include(gc => gc.AntiSpamSetting)
                    .ThenInclude(x => x.IgnoredChannels)
                .ToList();

        /// <summary>
        /// Gets and creates if it doesn't exist a config for a guild.
        /// </summary>
        /// <param name="guildId"></param>
        /// <returns></returns>
        public GuildConfig For(ulong guildId, Func<DbSet<GuildConfig>, IQueryable<GuildConfig>> includes = null)
        {
            GuildConfig config;

            if (includes == null)
            {
                config = _set
                    .Include(gc => gc.FollowedStreams)
                    .Include(gc => gc.LogSetting)
                        .ThenInclude(ls => ls.IgnoredChannels)
                    .Include(gc => gc.FilterInvitesChannelIds)
                    .Include(gc => gc.FilterWordsChannelIds)
                    .Include(gc => gc.FilteredWords)
                    .Include(gc => gc.GenerateCurrencyChannelIds)
                    .Include(gc => gc.CommandCooldowns)
                    .FirstOrDefault(c => c.GuildId == guildId);
            }
            else
            {
                var set = includes(_set);
                config = set.FirstOrDefault(c => c.GuildId == guildId);
            }

            if (config == null)
            {
                _set.Add((config = new GuildConfig
                {
                    GuildId = guildId,
                    Permissions = Permissionv2.GetDefaultPermlist
                }));
                _context.SaveChanges();
            }
            else if (config.Permissions == null)
            {
                config.Permissions = Permissionv2.GetDefaultPermlist;
                _context.SaveChanges();
            }
            return config;
        }

        public GuildConfig LogSettingsFor(ulong guildId)
        {
            return _set.Include(gc => gc.LogSetting)
                            .ThenInclude(gc => gc.IgnoredChannels)
               .FirstOrDefault();
        }

        public IEnumerable<GuildConfig> OldPermissionsForAll()
        {
            var query = _set
                .Where(gc => gc.RootPermission != null)
                .Include(gc => gc.RootPermission);

            //todo this is possibly a disaster for performance
            //What i could do instead is count the number of permissions in the permission table for this guild
            // and make a for loop with those.
            // or just select permissions for this guild and manually chain them
            for (int i = 0; i < 60; i++)
            {
                query = query.ThenInclude(gc => gc.Next);
            }

            return query.ToList();
        }

        public IEnumerable<GuildConfig> Permissionsv2ForAll()
        {
            var query = _set
                .Include(gc => gc.Permissions);

            return query.ToList();
        }

        public IEnumerable<FollowedStream> GetAllFollowedStreams() =>
            _set.Include(gc => gc.FollowedStreams)
                .SelectMany(gc => gc.FollowedStreams)
                .ToList();

        public void SetCleverbotEnabled(ulong id, bool cleverbotEnabled)
        {
            var conf = _set.FirstOrDefault(gc => gc.GuildId == id);

            if (conf == null)
                return;

            conf.CleverbotEnabled = cleverbotEnabled;
        }
    }
}
