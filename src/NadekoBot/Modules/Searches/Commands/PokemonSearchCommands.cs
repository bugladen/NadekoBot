using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Models;
using Newtonsoft.Json;
using NLog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class PokemonSearchCommands : NadekoSubmodule
        {
            private static Dictionary<string, SearchPokemon> pokemons { get; } = new Dictionary<string, SearchPokemon>();
            private static Dictionary<string, SearchPokemonAbility> pokemonAbilities { get; } = new Dictionary<string, SearchPokemonAbility>();

            public const string PokemonAbilitiesFile = "data/pokemon/pokemon_abilities7.json";

            public const string PokemonListFile = "data/pokemon/pokemon_list7.json";
            private new static readonly Logger _log;

            static PokemonSearchCommands()
            {
                _log = LogManager.GetCurrentClassLogger();

                if (File.Exists(PokemonListFile))
                {
                    pokemons = JsonConvert.DeserializeObject<Dictionary<string, SearchPokemon>>(File.ReadAllText(PokemonListFile));
                }
                else
                    _log.Warn(PokemonListFile + " is missing. Pokemon abilities not loaded.");
                if (File.Exists(PokemonAbilitiesFile))
                    pokemonAbilities = JsonConvert.DeserializeObject<Dictionary<string, SearchPokemonAbility>>(File.ReadAllText(PokemonAbilitiesFile));
                else
                    _log.Warn(PokemonAbilitiesFile + " is missing. Pokemon abilities not loaded.");
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Pokemon([Remainder] string pokemon = null)
            {
                pokemon = pokemon?.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(pokemon))
                    return;

                foreach (var kvp in pokemons)
                {
                    if (kvp.Key.ToUpperInvariant() == pokemon.ToUpperInvariant())
                    {
                        var p = kvp.Value;
                        await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithTitle(kvp.Key.ToTitleCase())
                            .WithDescription(p.BaseStats.ToString())
                            .AddField(efb => efb.WithName(GetText("types")).WithValue(string.Join(",\n", p.Types)).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("height_weight")).WithValue(GetText("height_weight_val", p.HeightM, p.WeightKg)).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("abilities")).WithValue(string.Join(",\n", p.Abilities.Select(a => a.Value))).WithIsInline(true)));
                        return;
                    }
                }
                await ReplyErrorLocalized("pokemon_none").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task PokemonAbility([Remainder] string ability = null)
            {
                ability = ability?.Trim().ToUpperInvariant().Replace(" ", "");
                if (string.IsNullOrWhiteSpace(ability))
                    return;
                foreach (var kvp in pokemonAbilities)
                {
                    if (kvp.Key.ToUpperInvariant() == ability)
                    {
                        await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithTitle(kvp.Value.Name)
                            .WithDescription(string.IsNullOrWhiteSpace(kvp.Value.Desc) 
                                ? kvp.Value.ShortDesc
                                : kvp.Value.Desc)
                            .AddField(efb => efb.WithName(GetText("rating"))
                                                .WithValue(kvp.Value.Rating.ToString(_cultureInfo)).WithIsInline(true))
                            ).ConfigureAwait(false);
                        return;
                    }
                }
                await ReplyErrorLocalized("pokemon_ability_none").ConfigureAwait(false);
            }
        }
    }
}