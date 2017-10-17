using System;
using System.Collections.Generic;

namespace NadekoBot.Core.Services.Database.Models
{
    public class GuildConfig : DbEntity
    {
        public ulong GuildId { get; set; }

        public string Prefix { get; set; } = null;

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
        public Permission RootPermission { get; set; } = null;
        public List<Permissionv2> Permissions { get; set; }
        public bool VerbosePermissions { get; set; } = true;
        public string PermissionRole { get; set; } = "Nadeko";

        public HashSet<CommandCooldown> CommandCooldowns { get; set; } = new HashSet<CommandCooldown>();

        //filtering
        public bool FilterInvites { get; set; }
        public HashSet<FilterChannelId> FilterInvitesChannelIds { get; set; } = new HashSet<FilterChannelId>();

        //public bool FilterLinks { get; set; }
        //public HashSet<FilterLinksChannelId> FilterLinksChannels { get; set; } = new HashSet<FilterLinksChannelId>();
        
        public bool FilterWords { get; set; }
        public HashSet<FilteredWord> FilteredWords { get; set; } = new HashSet<FilteredWord>();
        public HashSet<FilterChannelId> FilterWordsChannelIds { get; set; } = new HashSet<FilterChannelId>();

        public HashSet<MutedUserId> MutedUsers { get; set; } = new HashSet<MutedUserId>();

        public string MuteRoleName { get; set; }
        public bool CleverbotEnabled { get; set; }
        public HashSet<GuildRepeater> GuildRepeaters { get; set; } = new HashSet<GuildRepeater>();

        public AntiRaidSetting AntiRaidSetting { get; set; }
        public AntiSpamSetting AntiSpamSetting { get; set; }

        public string Locale { get; set; } = null;
        public string TimeZoneId { get; set; } = null;

        public HashSet<UnmuteTimer> UnmuteTimers { get; set; } = new HashSet<UnmuteTimer>();
        public HashSet<VcRoleInfo> VcRoleInfos { get; set; }
        public HashSet<CommandAlias> CommandAliases { get; set; } = new HashSet<CommandAlias>();
        public List<WarningPunishment> WarnPunishments { get; set; } = new List<WarningPunishment>();
        public bool WarningsInitialized { get; set; }
        public HashSet<SlowmodeIgnoredUser> SlowmodeIgnoredUsers { get; set; }
        public HashSet<SlowmodeIgnoredRole> SlowmodeIgnoredRoles { get; set; }
        public HashSet<NsfwBlacklitedTag> NsfwBlacklistedTags { get; set; } = new HashSet<NsfwBlacklitedTag>();

        public List<ShopEntry> ShopEntries { get; set; }
        public ulong? GameVoiceChannel { get; set; } = null;
        public bool VerboseErrors { get; set; } = false;

        public StreamRoleSettings StreamRole { get; set; }

        public XpSettings XpSettings { get; set; }
        public List<FeedSub> FeedSubs { get; set; } = new List<FeedSub>();

        //public List<ProtectionIgnoredChannel> ProtectionIgnoredChannels { get; set; } = new List<ProtectionIgnoredChannel>();
    }

    public class NsfwBlacklitedTag : DbEntity
    {
        public string Tag { get; set; }

        public override int GetHashCode()
        {
            return Tag.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is NsfwBlacklitedTag x
                ? x.Tag == Tag
                : false;
        }
    }

    public class SlowmodeIgnoredUser : DbEntity
    {
        public ulong UserId { get; set; }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return ((SlowmodeIgnoredUser)obj).UserId == UserId;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return UserId.GetHashCode();
        }
    }

    public class SlowmodeIgnoredRole : DbEntity
    {
        public ulong RoleId { get; set; }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return ((SlowmodeIgnoredRole)obj).RoleId == RoleId;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return RoleId.GetHashCode();
        }
    }

    public class WarningPunishment : DbEntity
    {
        public int Count { get; set; }
        public PunishmentAction Punishment { get; set; }
        public int Time { get; set; }
    }

    public class CommandAlias : DbEntity
    {
        public string Trigger { get; set; }
        public string Mapping { get; set; }

        //// override object.Equals
        //public override bool Equals(object obj)
        //{
        //    if (obj == null || GetType() != obj.GetType())
        //    {
        //        return false;
        //    }

        //    return ((CommandAlias)obj).Trigger.Trim().ToLowerInvariant() == Trigger.Trim().ToLowerInvariant();
        //}

        //// override object.GetHashCode
        //public override int GetHashCode()
        //{
        //    return Trigger.Trim().ToLowerInvariant().GetHashCode();
        //}
    }

    public class VcRoleInfo : DbEntity
    {
        public ulong VoiceChannelId { get; set; }
        public ulong RoleId { get; set; }
    }

    public class UnmuteTimer : DbEntity
    {
        public ulong UserId { get; set; }
        public DateTime UnmuteAt { get; set; }

        public override int GetHashCode() =>
            UserId.GetHashCode();

        public override bool Equals(object obj)
        {
            var ut = obj as UnmuteTimer;
            if (ut == null)
                return false;
            return ut.UserId == UserId;
        }
    }

    public class FilterChannelId : DbEntity
    {
        public ulong ChannelId { get; set; }
    }

    public class FilterLinksChannelId : DbEntity
    {
        public ulong ChannelId { get; set; }

        public override bool Equals(object obj)
        {
            return obj is FilterLinksChannelId f
                ? f.ChannelId == ChannelId
                : false;
        }

        public override int GetHashCode()
        {
            return ChannelId.GetHashCode();
        }
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
