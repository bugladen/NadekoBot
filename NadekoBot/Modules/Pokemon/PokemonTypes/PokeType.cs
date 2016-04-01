using NadekoBot.Modules.Pokemon.PokemonTypes;
using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.PokeTypes
{
    public interface PokeType
    {
        string Image { get; }
        string Name { get; }
        int Num { get; }
        double Multiplier(PokeType target);
    }
    public class PokemonTypesMain
    {

        public static PokeType stringToPokeType(string newType)
        {

            foreach (PokeType t in TypeList)
            {
                if (t.Name == newType)
                {
                    return t;
                }
            }
            return null;
        }

        //These classes can use all methods (except getMoves)
        public static List<PokeType> TypeList = new List<PokeType>()
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
            new SteelType(),
            new FairyType()
        };

        public static PokeType IntToPokeType(int id)
        {
            foreach (PokeType t in TypeList)
            {
                if (t.Num == id)
                {
                    return t;
                }
            }
            return null;
        }

    }
}
