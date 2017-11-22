using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.Common
{
    public class PokeStats
    {
        //Health left
        public int Hp { get; set; } = 500;
        public int MaxHp { get; } = 500;
        //Amount of moves made since last time attacked
        public int MovesMade { get; set; } = 0;
        //Last people attacked
        public List<ulong> LastAttacked { get; set; } = new List<ulong>();
    }
}
