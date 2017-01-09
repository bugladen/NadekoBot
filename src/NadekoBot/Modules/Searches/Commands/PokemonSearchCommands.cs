using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Models;
using Newtonsoft.Json;
using NLog;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class PokemonSearchCommands : ModuleBase
        {
            private static Dictionary<string, SearchPokemon> pokemons { get; } = new Dictionary<string, SearchPokemon>();
            private static Dictionary<string, SearchPokemonAbility> pokemonAbilities { get; } = new Dictionary<string, SearchPokemonAbility>();

            public const string PokemonAbilitiesFile = "data/pokemon/pokemon_abilities7.json";

            public const string PokemonListFile = "data/pokemon/pokemon_list7.json";
            private static Logger _log { get; }

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
                            .AddField(efb => efb.WithName("Types").WithValue(string.Join(",\n", p.Types)).WithIsInline(true))
                            .AddField(efb => efb.WithName("Height/Weight").WithValue($"{p.HeightM}m/{p.WeightKg}kg").WithIsInline(true))
                            .AddField(efb => efb.WithName("Abilitities").WithValue(string.Join(",\n", p.Abilities.Select(a => a.Value))).WithIsInline(true))
                            );
                        return;
                    }
                }
                await Context.Channel.SendErrorAsync("No pokemon found.");
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
                            .WithDescription(kvp.Value.Desc)
                            .AddField(efb => efb.WithName("Rating").WithValue(kvp.Value.Rating.ToString()).WithIsInline(true))
                            ).ConfigureAwait(false);
                        return;
                    }
                }
                await Context.Channel.SendErrorAsync("No ability found.");
            }
        }
    }
}