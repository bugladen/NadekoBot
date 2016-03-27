using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NadekoBot.Classes;
using NadekoBot.Classes._DataModels;
using NadekoBot.Modules.Pokemon.PokemonTypes;

namespace NadekoBot.Modules.Pokemon.PokeTypes
{

    public interface IPokeType
    {
        string getImage();
        string getName();
        int getNum();
        List<string> getMoves();
        double getMagnifier(IPokeType target);
        void updateMoves();
    }
    public class PokemonTypesMain
    {
        
        public static IPokeType stringToPokeType(string newType)
        {

            foreach (IPokeType t in typeList)
            {
                if (t.getName() == newType)
                {
                    return t;
                }
            }
            return null;
        }

        //These classes can use all methods (except getMoves)
        public static List<IPokeType> typeList = new List<IPokeType>()
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

        public static IPokeType intToPokeType(int id)
        {
            foreach (IPokeType t in typeList)
            {
                if (t.getNum() == id)
                {
                    return t;
                }
            }
            return null;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
       
        
        

    }
}
