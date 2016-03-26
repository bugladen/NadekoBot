using NadekoBot.Classes;
using NadekoBot.Classes._DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;



namespace NadekoBot.Modules.pokegame
{
    public class PokemonTypes
    {
        public interface PokeType
        {
            string getImage();
            string getName();
            int getNum();
            List<string> getMoves();
            double getMagnifier(PokeType target);
            void updateMoves();
        }
        public static PokeType stringToPokeType(string newType)
        {
            
            foreach (PokeType t in typeList)
            {
                if (t.getName() == newType)
                {
                    return t;
                }
            }
            return null;
        }

        public static List<PokeType> typeList = new List<PokeType>()
        {
            new Type_NORMAL(),
            new Type_FIRE(),
            new Type_WATER(),
            new Type_ELECTRIC(),
            new Type_GRASS(),
            new Type_ICE(),
            new Type_FIGHTING(),
            new Type_POISON(),
            new Type_GROUND(),
            new Type_FLYING(),
            new Type_PSYCHIC(),
            new Type_BUG(),
            new Type_ROCK(),
            new Type_GHOST(),
            new Type_DRAGON(),
            new Type_DARK(),
            new Type_STEEL()
        };

        public static PokeType intToPokeType(int id)
        {
            foreach(PokeType t in typeList)
            {
                if (t.getNum() == id)
                {
                    return t;
                }
            }
            return null;
        }

         class Type_NORMAL : PokeType
        {
            static readonly string name = "NORMAL";
            public static int type_num = 0;

            public double getMagnifier(PokeType target)
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
         class Type_FIRE : PokeType
        {
            static readonly string name = "FIRE";
            public static int type_num = 1;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
                {

                    case "FIRE": return 0.5;
                    case "WATER": return 0.5;
                    case "GRASS": return 2;
                    case "ICE": return 2;
                    case "BUG": return 2;
                    case "ROCK": return 0.5;
                    case "DRAGON": return 0.5;
                    case "STEEL": return 2;
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
                return "🔥";
            }

            public int getNum()
            {
                return type_num;
            }
        }
         class Type_WATER : PokeType
        {
            static readonly string name = "WATER";
            public static int type_num = 2;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
                {

                    case "FIRE": return 2;
                    case "WATER": return 0.5;
                    case "GRASS": return 0.5;
                    case "GROUND": return 2;
                    case "ROCK": return 2;
                    case "DRAGON": return 0.5;
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
                return "💦";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_ELECTRIC : PokeType
        {
            static readonly string name = "ELECTRIC";
            public static int type_num = 3;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
                {

                    case "WATER": return 2;
                    case "ELECTRIC": return 0.5;
                    case "GRASS": return 2;
                    case "GROUND": return 0;
                    case "FLYING": return 2;
                    case "DRAGON": return 0.5;
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
                return "⚡️";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_GRASS : PokeType
        {
            static readonly string name = "GRASS";
            public static int type_num = 4;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
                {

                    case "FIRE": return 0.5;
                    case "WATER": return 0.5;
                    case "GRASS": return 2;
                    case "ICE": return 2;
                    case "BUG": return 2;
                    case "ROCK": return 0.5;
                    case "DRAGON": return 0.5;
                    case "STEEL": return 2;
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
                return "🌿";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_ICE : PokeType
        {
            static readonly string name = "ICE";
            public static int type_num = 5;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
                {

                    case "FIRE": return 0.5;
                    case "WATER": return 0.5;
                    case "GRASS": return 2;
                    case "ICE": return 0.5;
                    case "GROUND": return 2;
                    case "FLYING": return 2;
                    case "DRAGON": return 2;
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
                return "❄";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_FIGHTING : PokeType
        {
            static readonly string name = "FIGHTING";
            public static int type_num = 6;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
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
                return "✊";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_POISON : PokeType
        {
            static readonly string name = "POISON";
            public static int type_num = 7;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
                {

                    case "GRASS": return 2;
                    case "POISON": return 0.5;
                    case "GROUND": return 0.5;
                    case "ROCK": return 0.5;
                    case "GHOST": return 0.5;
                    case "STEEL": return 0;
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
                return "☠";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_GROUND : PokeType
        {
            static readonly string name = "GROUND";
            public static int type_num = 8;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
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
                return "🗻";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_FLYING : PokeType
        {
            static readonly string name = "FLYING";
            public static int type_num = 9;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
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
                return "☁";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_PSYCHIC : PokeType
        {
            static readonly string name = "PSYCHIC";
            public static int type_num = 10;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
                {

                    case "FIGHTING": return 2;
                    case "POISON": return 2;
                    case "PSYCHIC": return 0.5;
                    case "DARK": return 0;
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
                return "💫";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_BUG : PokeType
        {
            static readonly string name = "BUG";
            public static int type_num = 11;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
                {

                    case "FIRE": return 0.5;
                    case "GRASS": return 2;
                    case "FIGHTING": return 0.5;
                    case "POISON": return 0.5;
                    case "FLYING": return 0.5;
                    case "PSYCHIC": return 2;
                    case "ROCK": return 0.5;
                    case "DARK": return 2;
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
                return "🐛";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_ROCK : PokeType
        {
            static readonly string name = "ROCK";
            public static int type_num = 12;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
                {

                    case "FIRE": return 2;
                    case "ICE": return 2;
                    case "FIGHTING": return 0.5;
                    case "GROUND": return 0.5;
                    case "FLYING": return 2;
                    case "BUG": return 2;
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
                return "💎";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_GHOST : PokeType
        {
            static readonly string name = "GHOST";
            public static int type_num = 13;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
                {

                    case "NORMAL": return 0;
                    case "PSYCHIC": return 2;
                    case "GHOST": return 2;
                    case "DARK": return 0.5;
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
                return "👻";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_DRAGON : PokeType
        {
            static readonly string name = "DRAGON";
            public static int type_num = 14;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
                {

                    case "DRAGON": return 2;
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
                return "🐉";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_DARK : PokeType
        {
            static readonly string name = "DARK";
            public static int type_num = 15;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
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
                return "🕶";
            }

            public int getNum()
            {
                return type_num;
            }
        }
        class Type_STEEL : PokeType
        {
            static readonly string name = "STEEL";
            public static int type_num = -1;

            public double getMagnifier(PokeType target)
            {
                switch (target.getName())
                {

                    case "FIRE": return 0.5;
                    case "WATER": return 0.5;
                    case "ELECTRIC": return 0.5;
                    case "ICE": return 2;
                    case "ROCK": return 2;
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
                return "🔩";
            }

            public int getNum()
            {
                return type_num;
            }
        }

    }
}

namespace PokeTypeExtensions
{

    public static class PokeTypeExtension
    {
        

        static int numberToType(this NadekoBot.Modules.pokegame.PokemonTypes.PokeType t)
        {
            return t.getNum();
        }
    }
}