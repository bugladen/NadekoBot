using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility.Models
{
    public class PatreonData
    {
        public JObject[] Included { get; set; }
        public JObject[] Data { get; set; }
        public PatreonDataLinks Links { get; set; }
    }

    public class PatreonDataLinks
    {
        public string first { get; set; }
        public string next { get; set; }
    }

    public class PatreonUserAndReward
    {
        public PatreonUser User { get; set; }
        public PatreonPledge Reward { get; set; }
    }
}
