using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class GrassType : PokeType
    {
        static readonly string name = "GRASS";
        public static int numType = 4;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "FIRE": return 0.5;
                case "WATER": return 0.5;
                case "GRASS": return 2;
                case "ICE": return 2;
                case "BUG": return 2;
                case "ROCK": return 0.5;
                case "DRAGON": return 0.5;
                case "STEEL": return 2;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();




        public string Name => name;


        public string Image => "🌿";

        public int Num => numType;
    }
}
