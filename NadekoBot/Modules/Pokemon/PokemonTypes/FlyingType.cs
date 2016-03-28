using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class FlyingType : PokeType
    {
        static readonly string name = "FLYING";
        public static int numType = 9;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "ELECTRIC": return 0.5;
                case "GRASS": return 2;
                case "FIGHTING": return 2;
                case "BUG": return 2;
                case "ROCK": return 0.5;
                case "STEEL": return 0.5;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();




        public string Name => name;



        public string Image => "☁";

        public int Num => numType;
    }
}
