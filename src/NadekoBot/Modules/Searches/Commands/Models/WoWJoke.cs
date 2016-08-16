using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches.Commands.Models
{
    public class WoWJoke
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public override string ToString() => $"`{Question}`\n\n**{Answer}**";
    }
}
