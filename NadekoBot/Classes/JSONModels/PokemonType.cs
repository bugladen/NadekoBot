using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes.JSONModels
{
    public class PokemonType
    {
        public PokemonType(string n, string i, string[] m, List<PokemonMultiplier> multi)
        {
            Name = n;
            Icon = i;
            Moves = m;
            Multipliers = multi;
        }
        public string Name { get; set; }
        public List<PokemonMultiplier> Multipliers { get; set; }
        public string Icon { get; set; }
        public string[] Moves { get; set; }
    }
    public class PokemonMultiplier
    {
        public PokemonMultiplier(string t, double m)
        {
            Type = t;
            Multiplication = m;
        }
        public string Type { get; set; }
        public double Multiplication { get; set; }
    }
}
