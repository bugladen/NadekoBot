using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class FairyType : PokeType
    {
        static readonly string name = "FAIRY";
        public static int numType = 17;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "FIGHTING": return 2;
                case "FIRE": return 0.5;
                case "DARK": return 0.5;
                case "POISON": return 0.5;
                case "STEEL": return 2;
                case "DRAGON": return 2;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();

        public string Name => name;

        public string Image => "💫";

        public int Num => numType;
    }
}
