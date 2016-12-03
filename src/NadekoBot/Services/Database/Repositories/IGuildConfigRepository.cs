using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IGuildConfigRepository : IRepository<GuildConfig>
    {
        GuildConfig For(ulong guildId, Func<DbSet<GuildConfig>, IQueryable<GuildConfig>> includes = null);
        GuildConfig PermissionsFor(ulong guildId);
        IEnumerable<GuildConfig> PermissionsForAll();
        GuildConfig SetNewRootPermission(ulong guildId, Permission p);
        IEnumerable<FollowedStream> GetAllFollowedStreams();
        void SetCleverbotEnabled(ulong id, bool cleverbotEnabled);
    }
}
