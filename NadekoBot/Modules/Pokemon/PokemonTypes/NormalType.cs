using NadekoBot.Modules.Pokemon.PokeTypes;
using System.Collections.Generic;


namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class NormalType : PokeType
    {
        static readonly string name = "NORMAL";
        public static int type_num = 0;

        public double Multiplier(PokeType target)
        {
            switch (target.Name)
            {
                case "ROCK": return 0.5;
                case "GHOST": return 0;
                case "STEEL": return 0.5;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();

        public string Name => name;

        public string Image => "⭕️";

        public int Num => type_num;
    }
}
