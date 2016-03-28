using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NadekoBot.Modules.Pokemon;
using NadekoBot.Classes;
using NadekoBot.Classes._DataModels; using NadekoBot.Modules.Pokemon.PokeTypes; 

namespace NadekoBot.Modules.Pokemon.PokemonTypes
{
    class GroundType : IPokeType
    {
        static readonly string name = "GROUND";
        public static int numType = 8;

        public double GetMagnifier(IPokeType target)
        {
            switch (target.GetName())
            {

                case "FIRE": return 2;
                case "ELECTRIC": return 2;
                case "GRASS": return 0.5;
                case "POISON": return 0.5;
                case "FLYING": return 0;
                case "BUG": return 0.5;
                case "ROCK": return 2;
                case "STEEL": return 2;
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
            return "🗻";
        }

        public int GetNum()
        {
            return numType;
        }
    }
}
