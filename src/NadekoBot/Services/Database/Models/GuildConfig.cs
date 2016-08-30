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

        //self assignable roles
        public bool ExclusiveSelfAssignedRoles { get; set; }
        public bool AutoDeleteSelfAssignedRoleMessages { get; set; }
        public float DefaultMusicVolume { get; set; } = 1.0f;
        public bool VoicePlusTextEnabled { get; set; }
    }
}
