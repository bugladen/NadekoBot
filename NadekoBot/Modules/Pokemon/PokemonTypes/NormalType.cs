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

        public double getMagnifier(IPokeType target)
        {
            switch (target.getName())
            {

                case "ROCK": return 0.5;
                case "GHOST": return 0;
                case "STEEL": return 0.5;
                default: return 1;
            }
        }
        List<string> moves = new List<string>();

        public List<string> getMoves()
        {
            updateMoves();
            return moves;
        }


        public string getName()
        {
            return name;
        }

        public void updateMoves()
        {
            var db = DbHandler.Instance.GetAllRows<PokeMoves>();
            foreach (PokeMoves p in db)
            {
                if (p.type == type_num)
                {
                    if (!moves.Contains(p.move))
                    {
                        moves.Add(p.move);
                    }
                }
            }
        }

        public string getImage()
        {
            return "⭕️";
        }

        public int getNum()
        {
            return type_num;
        }
    }
}
