using Discord.Commands;
using NadekoBot.Classes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace NadekoBot.Modules.Searches.Commands
{
    class PokemonSearchCommands : DiscordCommand
    {
        private static Dictionary<string, SearchPokemon> pokemons;
        private static Dictionary<string, SearchPokemonAbility> pokemonAbilities;

        public PokemonSearchCommands(DiscordModule module) : base(module)
        {

            pokemons = JsonConvert.DeserializeObject<Dictionary<string, SearchPokemon>>(File.ReadAllText("data/pokemon/pokemon_list.json"));
            pokemonAbilities = JsonConvert.DeserializeObject<Dictionary<string, SearchPokemonAbility>>(File.ReadAllText("data/pokemon/pokemon_abilities.json"));
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Prefix + "pokemon")
                .Alias(Prefix + "poke")
                .Description($"Searches for a pokemon. | `{Prefix}poke Sylveon`")
                .Parameter("pokemon", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var pok = e.GetArg("pokemon")?.Trim().ToUpperInvariant();
                    if (string.IsNullOrWhiteSpace(pok))
                        return;

                    foreach (var kvp in pokemons)
                    {
                        if (kvp.Key.ToUpperInvariant() == pok.ToUpperInvariant())
                        {
                            await e.Channel.SendMessage($"`Stats for \"{kvp.Key}\" pokemon:`\n{kvp.Value}");
                            return;
                        }
                    }
                    await e.Channel.SendMessage("`No pokemon found.`");
                });

            cgb.CreateCommand(Prefix + "pokemonability")
                .Alias(Prefix + "pokeab")
                .Description($"Searches for a pokemon ability. | `{Prefix}pokeab \"water gun\"`")
                .Parameter("abil", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var ab = e.GetArg("abil")?.Trim().ToUpperInvariant().Replace(" ", "");
                    if (string.IsNullOrWhiteSpace(ab))
                        return;
                    foreach (var kvp in pokemonAbilities)
                    {
                        if (kvp.Key.ToUpperInvariant() == ab)
                        {
                            await e.Channel.SendMessage($"`Info for \"{kvp.Key}\" ability:`\n{kvp.Value}");
                            return;
                        }
                    }
                    await e.Channel.SendMessage("`No ability found.`");
                });
        }
    }

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

            public override string ToString() => $@"
    **HP:**  {HP,-4} **ATK:** {ATK,-4} **DEF:** {DEF,-4}
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

        public override string ToString() => $@"`Name:` {Species}
`Types:` {string.Join(", ", Types)}
`Stats:` {BaseStats}
`Height:` {HeightM,4}m `Weight:` {WeightKg}kg
`Abilities:` {string.Join(", ", Abilities.Values)}";

    }

    public class SearchPokemonAbility
    {
        public string Desc { get; set; }
        public string Name { get; set; }
        public float Rating { get; set; }

        public override string ToString() => $@"`Name:` : {Name}
`Rating:` {Rating}
`Description:` {Desc}";
    }
}
