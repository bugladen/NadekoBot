using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class StreamRoleSettings : DbEntity
    {
        public int GuildConfigId { get; set; }
        public GuildConfig GuildConfig { get; set; }

        /// <summary>
        /// Id of the role to give to the users in the role 'FromRole' when they start streaming
        /// </summary>
        public ulong AddRoleId { get; set; }
        /// <summary>
        /// Id of the role whose users are eligible to get the 'AddRole'
        /// </summary>
        public ulong FromRoleId { get; set; }
    }
}
