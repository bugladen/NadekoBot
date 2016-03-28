using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class RockType : PokeType
    {
        static readonly string name = "ROCK";
        public static int numType = 12;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "FIRE": return 2;
                case "ICE": return 2;
                case "FIGHTING": return 0.5;
                case "GROUND": return 0.5;
                case "FLYING": return 2;
                case "BUG": return 2;
                case "STEEL": return 0.5;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();

        public string Name => name;

        public string Image => "💎";

        public int Num => numType;
    }
}
