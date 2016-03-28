using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class DragonType : PokeType
    {
        static readonly string name = "DRAGON";
        public static int numType = 14;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "DRAGON": return 2;
                case "STEEL": return 0.5;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();




        public string Name => name;



        public string Image => "🐉";

        public int Num => numType;
    }
}
