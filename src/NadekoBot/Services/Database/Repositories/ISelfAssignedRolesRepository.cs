using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Repositories
{
    public interface ISelfAssignedRolesRepository : IRepository<SelfAssignedRole>
    {
        bool DeleteByGuildAndRoleId(ulong guildId, ulong roleId);
        IEnumerable<SelfAssignedRole> GetFromGuild(ulong guildId);
    }
}
