using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class Permission : DbEntity
    {
        public PrimaryPermissionType PrimaryTarget { get; set; }
        public ulong PrimaryTargetId { get; set; }

        public SecondaryPermissionType SecondaryTarget { get; set; }
        public string SecondaryTargetName { get; set; }

        public bool State { get; set; }
    }

    public enum PrimaryPermissionType
    {
        User,
        Channel,
        Role
    }

    public enum SecondaryPermissionType
    {
        Module,
        Command
    }
}
