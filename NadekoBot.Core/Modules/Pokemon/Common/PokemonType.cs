using System.Collections.Generic;

namespace NadekoBot.Modules.Pokemon.Common
{
    public class PokemonType
    {
        public string Name { get; set; }
        public List<PokemonMultiplier> Multipliers { get; set;  }
        public string Icon { get; set; }
        public string[] Moves { get; set; }

        public override string ToString() =>
            Icon + "**" + Name.ToLowerInvariant() + "**" + Icon;
    }
    public class PokemonMultiplier
    {
        public string Type { get; set; }
        public double Multiplication { get; set; }
    }
}
