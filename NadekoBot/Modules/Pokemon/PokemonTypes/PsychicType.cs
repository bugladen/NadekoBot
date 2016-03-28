using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class PsychicType : PokeType
    {
        static readonly string name = "PSYCHIC";
        public static int numType = 10;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "FIGHTING": return 2;
                case "POISON": return 2;
                case "PSYCHIC": return 0.5;
                case "DARK": return 0;
                case "STEEL": return 0.5;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();




        public string Name => name;

        public string Image => "💫";

        public int Num => numType;
    }
}
