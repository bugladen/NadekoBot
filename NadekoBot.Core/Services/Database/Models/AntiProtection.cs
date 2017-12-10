using System.Collections.Generic;
namespace NadekoBot.Core.Services.Database.Models
{
    public class AntiRaidSetting : DbEntity
    {
        public int GuildConfigId { get; set; }
        public GuildConfig GuildConfig { get; set; }

        public int UserThreshold { get; set; }
        public int Seconds { get; set; }
        public PunishmentAction Action { get; set; }
    }

    public class AntiSpamSetting : DbEntity
    {
        public int GuildConfigId { get; set; }
        public GuildConfig GuildConfig { get; set; }

        public PunishmentAction Action { get; set; }
        public int MessageThreshold { get; set; } = 3;
        public int MuteTime { get; set; } = 0;
        public HashSet<AntiSpamIgnore> IgnoredChannels { get; set; } = new HashSet<AntiSpamIgnore>();
    }


    public enum PunishmentAction
    {
        Mute,
        Kick,
        Ban,
        Softban,
        RemoveRoles,
    }

    public class AntiSpamIgnore : DbEntity
    {
        public ulong ChannelId { get; set; }

        public override int GetHashCode() => ChannelId.GetHashCode();

        public override bool Equals(object obj)
        {
            var inst = obj as AntiSpamIgnore;

            if (inst == null)
                return false;

            return inst.ChannelId == ChannelId;
            
        }
    }
}