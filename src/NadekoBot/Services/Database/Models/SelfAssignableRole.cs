using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class SelfAssignedRole : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
    }
}
