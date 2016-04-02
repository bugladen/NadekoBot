using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes.JSONModels
{
    public class PokeMove
    {
        public PokeMove(string n, string t)
        {
            name = n;
            type = t;
        }
        public string name { get; set; } = "";
        public string type { get; set; } = "";
    }
}
