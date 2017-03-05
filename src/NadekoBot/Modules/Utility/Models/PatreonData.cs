using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility.Models
{

    public class PatreonData
    {
        public Pledge[] Data { get; set; }
        public Links Links { get; set; }
    }

    public class Attributes
    {
        public int amount_cents { get; set; }
        public string created_at { get; set; }
        public string declined_since { get; set; }
        public bool is_twitch_pledge { get; set; }
        public bool patron_pays_fees { get; set; }
        public int pledge_cap_cents { get; set; }
    }

    public class Pledge
    {
        public Attributes Attributes { get; set; }
        public int Id { get; set; }
    }

    public class Links
    {
        public string First { get; set; }
        public string Next { get; set; }
    }
}
