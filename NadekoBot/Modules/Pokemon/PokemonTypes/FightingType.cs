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
    class FightingType : IPokeType
    {
        static readonly string name = "FIGHTING";
        public static int numType = 6;

        public double GetMagnifier(IPokeType target)
        {
            switch (target.GetName())
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

        


        public string GetName()
        {
            return name;
        }

        
        public string GetImage()
        {
            return "✊";
        }

        public int GetNum()
        {
            return numType;
        }
    }
}
