using System.Collections.Generic;

namespace NadekoBot.Services.Database.Models
{
    public class LogSetting : DbEntity
    {
        public bool IsLogging { get; set; }
        public ulong ChannelId { get; set; }
        public HashSet<IgnoredLogChannel> IgnoredChannels { get; set; }

        public bool MessageUpdated { get; set; } = true;
        public bool MessageDeleted { get; set; } = true;

        public bool UserJoined { get; set; } = true;
        public bool UserLeft { get; set; } = true;
        public bool UserBanned { get; set; } = true;
        public bool UserUnbanned { get; set; } = true;
        public bool UserUpdated { get; set; } = true;

        public bool ChannelCreated { get; set; } = true;
        public bool ChannelDestroyed { get; set; } = true;
        public bool ChannelUpdated { get; set; } = true;

        //userpresence
        public bool LogUserPresence { get; set; } = false;
        public ulong UserPresenceChannelId { get; set; }

        //voicepresence
        public bool LogVoicePresence { get; set; } = false;
        public ulong VoicePresenceChannelId { get; set; }
        public HashSet<IgnoredVoicePresenceChannel> IgnoredVoicePresenceChannelIds { get; set; }

    }
}
