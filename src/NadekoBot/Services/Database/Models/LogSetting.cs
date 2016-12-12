using System.Collections.Generic;

namespace NadekoBot.Services.Database.Models
{
    public class LogSetting : DbEntity
    {
        public HashSet<IgnoredLogChannel> IgnoredChannels { get; set; }

        public ulong? LogOtherId { get; set; } = null;
        public ulong? MessageUpdatedId { get; set; } = null;
        public ulong? MessageDeletedId { get; set; } = null;
               
        public ulong? UserJoinedId { get; set; } = null;
        public ulong? UserLeftId { get; set; } = null;
        public ulong? UserBannedId { get; set; } = null;
        public ulong? UserUnbannedId { get; set; } = null;
        public ulong? UserUpdatedId { get; set; } = null;
               
        public ulong? ChannelCreatedId { get; set; } = null;
        public ulong? ChannelDestroyedId { get; set; } = null;
        public ulong? ChannelUpdatedId { get; set; } = null;

        //userpresence
        public ulong? LogUserPresenceId { get; set; } = null;

        //voicepresence
        public ulong? LogVoicePresenceId { get; set; } = null;
        public ulong? LogVoicePresenceTTSId { get; set; } = null;
        public HashSet<IgnoredLogChannel> IgnoredVoicePresenceChannels { get; set; }
    }
}
