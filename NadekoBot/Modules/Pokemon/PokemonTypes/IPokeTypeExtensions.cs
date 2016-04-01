using NadekoBot.Classes;
using NadekoBot.Classes._DataModels;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokeTypes.Extensions
{
    public static class IPokeTypeExtensions
    {
        public static List<string> GetMoves(this PokeType poketype)
        {
            var db = DbHandler.Instance.GetAllRows<PokeMoves>();
            List<string> moves = new List<string>();
            foreach (PokeMoves p in db)
            {
                if (p.type == poketype.Num)
                {
                    if (!moves.Contains(p.move))
                    {
                        moves.Add(p.move);
                    }
                }
            }
            return moves;
        }
    }

}
