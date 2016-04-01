using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class PoisonType : PokeType
    {
        static readonly string name = "POISON";
        public static int numType = 7;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "GRASS": return 2;
                case "POISON": return 0.5;
                case "GROUND": return 0.5;
                case "ROCK": return 0.5;
                case "GHOST": return 0.5;
                case "STEEL": return 0;
                case "FAIRY": return 2;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();

        public string Name => name;

        public string Image => "☠";

        public int Num => numType;
    }
}
