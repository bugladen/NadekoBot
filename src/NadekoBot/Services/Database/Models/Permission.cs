using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class Permission : DbEntity
    {
        public PermissionType TargetType { get; set; }
        public string Command { get; set; } = null;
        public string Module { get; set; } = null;
        public bool State { get; set; }
        public string Target { get; set; }
    }

    public enum PermissionType
    {
        User,
        Channel,
        Role
    }
}
