using NadekoBot.Classes;
using NadekoBot.Classes._DataModels;
using NadekoBot.Classes.JSONModels;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokeTypes.Extensions
{
    public static class IPokeTypeExtensions
    {
        public static List<string> GetMoves(this PokeType poketype)
        {
            var moveSet = NadekoBot.Config.PokemonMoves;
            List<string> moves = new List<string>();
            foreach (PokeMove p in moveSet)
            {
                if (p.type == poketype.Name)
                {
                    if (!moves.Contains(p.name))
                    {
                        moves.Add(p.name);
                    }
                }
            }
            return moves;
        }
    }

}
