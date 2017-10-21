using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace NadekoBot.Core.Services.Database.Models
{
    [DebuggerDisplay("{global::NadekoBot.Modules.Permissions.PermissionExtensions.GetCommand(this)}", Target = typeof(Permission))]
    public class Permission : DbEntity
    {
        public Permission Previous { get; set; } = null;
        public Permission Next { get; set; } = null;

        public PrimaryPermissionType PrimaryTarget { get; set; }
        public ulong PrimaryTargetId { get; set; }

        public SecondaryPermissionType SecondaryTarget { get; set; }
        public string SecondaryTargetName { get; set; }

        public bool State { get; set; }

        public Permissionv2 Tov2() =>
            new Permissionv2()
            {
                PrimaryTarget = PrimaryTarget,
                PrimaryTargetId = PrimaryTargetId,
                SecondaryTarget = SecondaryTarget,
                SecondaryTargetName = SecondaryTargetName,
                State = State,
            };

        //[NotMapped]
        //private static Permission AllowAllPerm => new Permission()
        //{
        //    PrimaryTarget = PrimaryPermissionType.Server,
        //    PrimaryTargetId = 0,
        //    SecondaryTarget = SecondaryPermissionType.AllModules,
        //    SecondaryTargetName = "*",
        //    State = true,
        //};
        //[NotMapped]
        //private static Permission BlockNsfwPerm => new Permission()
        //{
        //    PrimaryTarget = PrimaryPermissionType.Server,
        //    PrimaryTargetId = 0,
        //    SecondaryTarget = SecondaryPermissionType.Module,
        //    SecondaryTargetName = "nsfw",
        //    State = false,
        //};

        //public Permission Clone() => new Permission()
        //{
        //    PrimaryTarget = PrimaryTarget,
        //    SecondaryTarget = SecondaryTarget,
        //    PrimaryTargetId = PrimaryTargetId,
        //    SecondaryTargetName = SecondaryTargetName,
        //    State = State,
        //};
    }

    public interface IIndexed
    {
        int Index { get; set; }
    }

    [DebuggerDisplay("{PrimaryTarget}{SecondaryTarget} {SecondaryTargetName} {State} {PrimaryTargetId}")]
    public class Permissionv2 : DbEntity, IIndexed
    {
        public int? GuildConfigId { get; set; }
        public int Index { get; set; }

        public PrimaryPermissionType PrimaryTarget { get; set; }
        public ulong PrimaryTargetId { get; set; }

        public SecondaryPermissionType SecondaryTarget { get; set; }
        public string SecondaryTargetName { get; set; }

        public bool State { get; set; }

        [NotMapped]
        public static Permissionv2 AllowAllPerm => new Permissionv2()
        {
            PrimaryTarget = PrimaryPermissionType.Server,
            PrimaryTargetId = 0,
            SecondaryTarget = SecondaryPermissionType.AllModules,
            SecondaryTargetName = "*",
            State = true,
            Index = 0,
        };
        [NotMapped]
        private static Permissionv2 BlockNsfwPerm => new Permissionv2()
        {
            PrimaryTarget = PrimaryPermissionType.Server,
            PrimaryTargetId = 0,
            SecondaryTarget = SecondaryPermissionType.Module,
            SecondaryTargetName = "nsfw",
            State = false,
            Index = 1
        };

        public static List<Permissionv2> GetDefaultPermlist =>
            new List<Permissionv2>
            {
                BlockNsfwPerm,
                AllowAllPerm
            };

        //public Permission Clone() => new Permission()
        //{
        //    PrimaryTarget = PrimaryTarget,
        //    SecondaryTarget = SecondaryTarget,
        //    PrimaryTargetId = PrimaryTargetId,
        //    SecondaryTargetName = SecondaryTargetName,
        //    State = State,
        //};
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
