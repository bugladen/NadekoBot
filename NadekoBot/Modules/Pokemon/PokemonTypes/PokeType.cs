using System.Collections.Generic;
using NadekoBot.Classes;
using NadekoBot.Classes._DataModels;
using NadekoBot.Modules.Pokemon.PokemonTypes;

namespace NadekoBot.Modules.Pokemon.PokeTypes
{

    public interface IPokeType
    {

        string GetImage();
        string GetName();
        int GetNum();
        double GetMagnifier(IPokeType target);
    }
    public class PokemonTypesMain
    {
        
        public static IPokeType stringToPokeType(string newType)
        {

            foreach (IPokeType t in TypeList)
            {
                if (t.GetName() == newType)
                {
                    return t;
                }
            }
            return null;
        }

        //public static List<string> getMoves(int numType)
        //{
        //    var db = DbHandler.Instance.GetAllRows<PokeMoves>();
        //    List<string> moves = new List<string>();
        //    foreach (PokeMoves p in db)
        //    {
        //        if (p.type == numType)
        //        {
        //            if (!moves.Contains(p.move))
        //            {
        //                moves.Add(p.move);
        //            }
        //        }
        //    }
        //    return moves;
        //}


        //These classes can use all methods (except getMoves)
        public static List<IPokeType> TypeList = new List<IPokeType>()
        {
            new NormalType(),
            new FireType(),
            new WaterType(),
            new ElectricType(),
            new GrassType(),
            new IceType(),
            new FightingType(),
            new PoisonType(),
            new GroundType(),
            new FlyingType(),
            new PsychicType(),
            new BugType(),
            new RockType(),
            new GhostType(),
            new DragonType(),
            new DarkType(),
            new SteelType()
        };

        public static IPokeType IntToPokeType(int id)
        {
            foreach (IPokeType t in TypeList)
            {
                if (t.GetNum() == id)
                {
                    return t;
                }
            }
            return null;
        }

    }
}
