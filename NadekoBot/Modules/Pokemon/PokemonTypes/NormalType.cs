using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NadekoBot.Modules.Pokemon;
using NadekoBot.Classes;
using NadekoBot.Classes._DataModels;
using NadekoBot.Modules.Pokemon.PokeTypes;


namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class NormalType : IPokeType
    {
        static readonly string name = "NORMAL";
        public static int type_num = 0;

        public double GetMagnifier(IPokeType target)
        {
            switch (target.GetName())
            {

                case "ROCK": return 0.5;
                case "GHOST": return 0;
                case "STEEL": return 0.5;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();

        


        public string GetName()
        {
            return name;
        }

        

        public string GetImage()
        {
            return "⭕️";
        }

        public int GetNum()
        {
            return type_num;
        }
    }
}
