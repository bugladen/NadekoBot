using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class ClashCaller : DbEntity
    {
        public string CallUser { get; set; }

        public DateTime TimeAdded { get; set; }

        public bool BaseDestroyed { get; set; }

        public int Stars { get; set; } = 3;

        public int ClashWarId { get; set; }

        [ForeignKey(nameof(ClashWarId))]
        public ClashWar ClashWar { get; set; }
    }
}
