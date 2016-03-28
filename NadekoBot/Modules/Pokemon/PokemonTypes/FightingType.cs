using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class FightingType : PokeType
    {
        static readonly string name = "FIGHTING";
        public static int numType = 6;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {

                case "NORMAL": return 2;
                case "ICE": return 2;
                case "POISON": return 0.5;
                case "FLYING": return 0.5;
                case "PSYCHIC": return 0.5;
                case "BUG": return 0.5;
                case "ROCK": return 2;
                case "GHOST": return 0;
                case "DARK": return 2;
                case "STEEL": return 2;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();




        public string Name => name;


        public string Image => "✊";

        public int Num => numType;
    }
}
