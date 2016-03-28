using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class GhostType : PokeType
    {
        static readonly string name = "GHOST";
        public static int numType = 13;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "NORMAL": return 0;
                case "PSYCHIC": return 2;
                case "GHOST": return 2;
                case "DARK": return 0.5;
                case "STEEL": return 0.5;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();




        public string Name => name;



        public string Image => "👻";

        public int Num => numType;
    }
}
