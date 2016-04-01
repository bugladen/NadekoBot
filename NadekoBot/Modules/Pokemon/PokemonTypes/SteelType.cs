using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class SteelType : PokeType
    {
        static readonly string name = "STEEL";
        public static int numType = 16;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "FIRE": return 0.5;
                case "WATER": return 0.5;
                case "ELECTRIC": return 0.5;
                case "ICE": return 2;
                case "ROCK": return 2;
                case "STEEL": return 0.5;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();

        public string Name => name;

        public string Image => "🔩";

        public int Num => numType;
    }
}
