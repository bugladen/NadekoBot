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
    class DarkType : IPokeType
    {
        static readonly string name = "DARK";
        public static int numType = 15;

        public double GetMagnifier(IPokeType target)
        {
            switch (target.GetName())
            {

                case "FIGHTING": return 0.5;
                case "PSYCHIC": return 2;
                case "GHOST": return 2;
                case "DARK": return 0.5;
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
            return "🕶";
        }

        public int GetNum()
        {
            return numType;
        }
    }
}
