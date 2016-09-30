using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class Permission : DbEntity
    {
        public Permission Previous { get; set; } = null;
        public Permission Next { get; set; } = null;

        public PrimaryPermissionType PrimaryTarget { get; set; }
        public ulong PrimaryTargetId { get; set; }

        public SecondaryPermissionType SecondaryTarget { get; set; }
        public string SecondaryTargetName { get; set; }

        public bool State { get; set; }

        [NotMapped]
        private static Permission AllowAllPerm => new Permission()
        {
            PrimaryTarget = PrimaryPermissionType.Server,
            PrimaryTargetId = 0,
            SecondaryTarget = SecondaryPermissionType.AllModules,
            SecondaryTargetName = "*",
            State = true,
        };
        [NotMapped]
        private static Permission BlockNsfwPerm => new Permission()
        {
            PrimaryTarget = PrimaryPermissionType.Server,
            PrimaryTargetId = 0,
            SecondaryTarget = SecondaryPermissionType.Module,
            SecondaryTargetName = "nsfw",
            State = false,
        };

        public static Permission GetDefaultRoot()
        {
            var root = AllowAllPerm;
            var blockNsfw = BlockNsfwPerm;

            root.Previous = blockNsfw;
            blockNsfw.Next = root;

            return blockNsfw;
        }
    }

    public enum PrimaryPermissionType
    {
        User,
        Channel,
        Role,
        Server
    }

    public enum SecondaryPermissionType
    {
        Module,
        Command,
        AllModules
    }
}
