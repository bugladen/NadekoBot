using System.Collections.Generic;

namespace NadekoBot.Modules.Searches.Common
{
    public class SearchPokemon
    {
        public class GenderRatioClass
        {
            public float M { get; set; }
            public float F { get; set; }
        }
        public class BaseStatsClass
        {
            public int HP { get; set; }
            public int ATK { get; set; }
            public int DEF { get; set; }
            public int SPA { get; set; }
            public int SPD { get; set; }
            public int SPE { get; set; }

            public override string ToString() => $@"**HP:**  {HP,-4} **ATK:** {ATK,-4} **DEF:** {DEF,-4}
**SPA:** {SPA,-4} **SPD:** {SPD,-4} **SPE:** {SPE,-4}";
        }
        public int Id { get; set; }
        public string Species { get; set; }
        public string[] Types { get; set; }
        public GenderRatioClass GenderRatio { get; set; }
        public BaseStatsClass BaseStats { get; set; }
        public Dictionary<string, string> Abilities { get; set; }
        public float HeightM { get; set; }
        public float WeightKg { get; set; }
        public string Color { get; set; }
        public string[] Evos { get; set; }
        public string[] EggGroups { get; set; }

//        public override string ToString() => $@"`Name:` {Species}
//`Types:` {string.Join(", ", Types)}
//`Stats:` {BaseStats}
//`Height:` {HeightM,4}m `Weight:` {WeightKg}kg
//`Abilities:` {string.Join(", ", Abilities.Values)}";

    }

    public class SearchPokemonAbility
    {
        public string Desc { get; set; }
        public string ShortDesc { get; set; }
        public string Name { get; set; }
        public float Rating { get; set; }

//        public override string ToString() => $@"`Name:` : {Name}
//`Rating:` {Rating}
//`Description:` {Desc}";
    }
}
