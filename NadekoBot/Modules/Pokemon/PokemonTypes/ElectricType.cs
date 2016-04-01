using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class ElectricType : PokeType
    {
        static readonly string name = "ELECTRIC";
        public static int numType = 3;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "WATER": return 2;
                case "ELECTRIC": return 0.5;
                case "GRASS": return 2;
                case "GROUND": return 0;
                case "FLYING": return 2;
                case "DRAGON": return 0.5;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();




        public string Name => name;


        public string Image => "⚡️";

        public int Num => numType;
    }
}
