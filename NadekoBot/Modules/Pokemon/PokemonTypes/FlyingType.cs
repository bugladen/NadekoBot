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
    class FlyingType : IPokeType
    {
        static readonly string name = "FLYING";
        public static int numType = 9;

        public double GetMagnifier(IPokeType target)
        {
            switch (target.GetName())
            {

                case "ELECTRIC": return 0.5;
                case "GRASS": return 2;
                case "FIGHTING": return 2;
                case "BUG": return 2;
                case "ROCK": return 0.5;
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
            return "☁";
        }

        public int GetNum()
        {
            return numType;
        }
    }
}
