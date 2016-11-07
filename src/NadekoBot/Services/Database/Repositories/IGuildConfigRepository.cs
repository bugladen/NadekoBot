using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Database.Repositories.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IGuildConfigRepository : IRepository<GuildConfig>
    {
        GuildConfig For(ulong guildId);
        GuildConfig PermissionsFor(ulong guildId);
        IEnumerable<GuildConfig> PermissionsForAll();
        GuildConfig SetNewRootPermission(ulong guildId, Permission p);
        IEnumerable<FollowedStream> GetAllFollowedStreams();
        void SetCleverbotEnabled(ulong id, bool cleverbotEnabled);
    }
}
