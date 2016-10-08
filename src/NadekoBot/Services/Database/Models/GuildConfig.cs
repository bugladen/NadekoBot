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
        //greet stuff
        public bool AutoDeleteGreetMessages { get; set; }
        public bool AutoDeleteByeMessages { get; set; }
        public int AutoDeleteGreetMessagesTimer { get; set; } = 30;

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
        public List<FollowedStream> FollowedStreams { get; set; } = new List<FollowedStream>();

        //currencyGeneration
        public ulong? GenerateCurrencyChannelId { get; set; }

        //permissions
        public Permission RootPermission { get; set; }
        public bool VerbosePermissions { get; set; } = true;
        public string PermissionRole { get; set; } = "Nadeko";

        public HashSet<CommandCooldown> CommandCooldowns { get; set; } = new HashSet<CommandCooldown>();

        //filtering
        public bool FilterInvites { get; set; }
        public HashSet<FilterChannelId> FilterInvitesChannelIds { get; set; }

        public bool FilterWords { get; set; }
        public HashSet<FilteredWord> FilteredWords { get; set; }
        public HashSet<FilterChannelId> FilterWordsChannelIds { get; set; }
    }

    public class FilterChannelId :DbEntity
    {
        public ulong ChannelId { get; set; }
    }

    public class FilteredWord :DbEntity
    {
        public string Word { get; set; }
    }
}
