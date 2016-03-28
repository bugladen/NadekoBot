using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class GroundType : PokeType
    {
        static readonly string name = "GROUND";
        public static int numType = 8;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "FIRE": return 2;
                case "ELECTRIC": return 2;
                case "GRASS": return 0.5;
                case "POISON": return 0.5;
                case "FLYING": return 0;
                case "BUG": return 0.5;
                case "ROCK": return 2;
                case "STEEL": return 2;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();

        public string Name => name;

        public string Image => "🗻";

        public int Num => numType;
    }
}
