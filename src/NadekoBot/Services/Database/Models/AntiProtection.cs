using Discord;
using NadekoBot.Services.Database.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
namespace NadekoBot.Services.Database.Models
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
        public HashSet<AntiSpamIgnore> IgnoredChannels { get; set; } = new HashSet<AntiSpamIgnore>();
    }


    public enum PunishmentAction
    {
        Mute,
        Kick,
        Ban,
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