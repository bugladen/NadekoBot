using Discord;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace NadekoBot.Services.Database.Models
{
    public class ClashWar : DbEntity
    {
        public string EnemyClan { get; set; }
        public int Size { get; set; }
        public StateOfWar WarState { get; set; } = StateOfWar.Created;
        public DateTime StartedAt { get; set; }

        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }

        [NotMapped]
        public ITextChannel Channel { get; set; }

        public List<ClashCaller> Bases { get; set; } = new List<ClashCaller>();
    }

    public enum DestroyStars
    {
        One, Two, Three
    }
    public enum StateOfWar
    {
        Started, Ended, Created
    }
}
