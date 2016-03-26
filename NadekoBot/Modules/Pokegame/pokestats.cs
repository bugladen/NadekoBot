using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.pokegame
{
    class Pokestats
    {
        //Health left
        public int HP { get; set; } = 500;
        //Amount of moves made since last time attacked
        public int movesMade { get; set; } = 0;
        //Last people attacked
        public List<ulong> lastAttacked { get; set; } = new List<ulong>();
    }
}
