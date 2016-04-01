using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class DarkType : PokeType
    {
        static readonly string name = "DARK";
        public static int numType = 15;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "FIGHTING": return 0.5;
                case "PSYCHIC": return 2;
                case "GHOST": return 2;
                case "DARK": return 0.5;
                case "FAIRY": return 0.5;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();

        public string Name => name;

        public string Image => "🕶";

        public int Num => numType;
    }
}
