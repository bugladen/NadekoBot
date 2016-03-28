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
    class PoisonType : IPokeType
    {
        static readonly string name = "POISON";
        public static int numType = 7;

        public double GetMagnifier(IPokeType target)
        {
            switch (target.GetName())
            {

                case "GRASS": return 2;
                case "POISON": return 0.5;
                case "GROUND": return 0.5;
                case "ROCK": return 0.5;
                case "GHOST": return 0.5;
                case "STEEL": return 0;
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
            return "☠";
        }

        public int GetNum()
        {
            return numType;
        }
    }
}
