using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class GuildConfig : DbEntity
    {
        public ulong GuildId { get; set; }
        public bool DeleteMessageOnCommand { get; set; }
        public ulong AutoAssignRoleId { get; set; }
    }
}
