using System;

namespace NadekoBot.Core.Services.Database.Models
{
    public class UserXpStats : DbEntity
    {
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public int Xp { get; set; }
        public int AwardedXp { get; set; }
        public XpNotificationType NotifyOnLevelUp { get; set; }
        public DateTime LastLevelUp { get; set; } = DateTime.UtcNow;
    }

    public enum XpNotificationType { None, Dm, Channel }
}
