using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class IceType : PokeType
    {
        static readonly string name = "ICE";
        public static int numType = 5;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "FIRE": return 0.5;
                case "WATER": return 0.5;
                case "GRASS": return 2;
                case "ICE": return 0.5;
                case "GROUND": return 2;
                case "FLYING": return 2;
                case "DRAGON": return 2;
                case "STEEL": return 0.5;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();

        public string Name => name;

        public string Image => "❄";

        public int Num => numType;
    }
}
