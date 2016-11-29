using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NadekoBot.Services.Database.Models
{
    public class ClashCaller : DbEntity
    {
        public int? SequenceNumber { get; set; } = null;
        public string CallUser { get; set; }

        public DateTime TimeAdded { get; set; }

        public bool BaseDestroyed { get; set; }

        public int Stars { get; set; } = 3;

        public int ClashWarId { get; set; }

        [ForeignKey(nameof(ClashWarId))]
        public ClashWar ClashWar { get; set; }
    }
}
