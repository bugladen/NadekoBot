using System.Collections.Generic;

namespace NadekoBot.Services.Database.Models
{
    public class GuildConfig : DbEntity
    {
        public ulong GuildId { get; set; }
        public bool DeleteMessageOnCommand { get; set; }
        public ulong AutoAssignRoleId { get; set; }
        //greet stuff
        public bool AutoDeleteGreetMessages { get; set; } //unused
        public bool AutoDeleteByeMessages { get; set; } // unused
        public int AutoDeleteGreetMessagesTimer { get; set; } = 30;
        public int AutoDeleteByeMessagesTimer { get; set; } = 30;

        public ulong GreetMessageChannelId { get; set; }
        public ulong ByeMessageChannelId { get; set; }

        public bool SendDmGreetMessage { get; set; }
        public string DmGreetMessageText { get; set; } = "Welcome to the %server% server, %user%!";

        public bool SendChannelGreetMessage { get; set; }
        public string ChannelGreetMessageText { get; set; } = "Welcome to the %server% server, %user%!";

        public bool SendChannelByeMessage { get; set; }
        public string ChannelByeMessageText { get; set; } = "%user% has left!";

        public LogSetting LogSetting { get; set; } = new LogSetting();

        //self assignable roles
        public bool ExclusiveSelfAssignedRoles { get; set; }
        public bool AutoDeleteSelfAssignedRoleMessages { get; set; }
        public float DefaultMusicVolume { get; set; } = 1.0f;
        public bool VoicePlusTextEnabled { get; set; }

        //stream notifications
        public HashSet<FollowedStream> FollowedStreams { get; set; } = new HashSet<FollowedStream>();

        //currencyGeneration
        public HashSet<GCChannelId> GenerateCurrencyChannelIds { get; set; } = new HashSet<GCChannelId>();

        //permissions
        public Permission RootPermission { get; set; }
        public bool VerbosePermissions { get; set; } = true;
        public string PermissionRole { get; set; } = "Nadeko";

        public HashSet<CommandCooldown> CommandCooldowns { get; set; } = new HashSet<CommandCooldown>();

        //filtering
        public bool FilterInvites { get; set; }
        public HashSet<FilterChannelId> FilterInvitesChannelIds { get; set; } = new HashSet<FilterChannelId>();

        public bool FilterWords { get; set; }
        public HashSet<FilteredWord> FilteredWords { get; set; } = new HashSet<FilteredWord>();
        public HashSet<FilterChannelId> FilterWordsChannelIds { get; set; } = new HashSet<FilterChannelId>();

        public HashSet<MutedUserId> MutedUsers { get; set; } = new HashSet<MutedUserId>();

        public string MuteRoleName { get; set; }
        public bool CleverbotEnabled { get; set; }
    }

    public class FilterChannelId : DbEntity
    {
        public ulong ChannelId { get; set; }
    }

    public class FilteredWord : DbEntity
    {
        public string Word { get; set; }
    }

    public class MutedUserId : DbEntity
    {
        public ulong UserId { get; set; }

        public override int GetHashCode()
        {
            return UserId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var mui = obj as MutedUserId;
            if (mui == null)
                return false;

            return mui.UserId == this.UserId;
        }
    }

    public class GCChannelId : DbEntity
    {
        public ulong ChannelId { get; set; }

        public override bool Equals(object obj)
        {
            var gc = obj as GCChannelId;
            if (gc == null)
                return false;

            return gc.ChannelId == this.ChannelId;
        }

        public override int GetHashCode() =>
            this.ChannelId.GetHashCode();
    }
}
