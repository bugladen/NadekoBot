using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class BugType : PokeType
    {
        static readonly string name = "BUG";
        public static int numType = 11;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "FIRE": return 0.5;
                case "GRASS": return 2;
                case "FIGHTING": return 0.5;
                case "POISON": return 0.5;
                case "FLYING": return 0.5;
                case "GHOST": return 0.5;
                case "PSYCHIC": return 2;
                case "ROCK": return 0.5;
                case "FAIRY": return 0.5;
                case "DARK": return 2;
                case "STEEL": return 0.5;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();

        public string Name => name;

        public string Image => "🐛";

        public int Num => numType;
    }
}
