using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Searches.Common;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class PokemonSearchCommands : NadekoSubmodule<SearchesService>
        {
            public Dictionary<string, SearchPokemon> Pokemons => _service.Pokemons;
            public Dictionary<string, SearchPokemonAbility> PokemonAbilities => _service.PokemonAbilities;

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Pokemon([Remainder] string pokemon = null)
            {
                pokemon = pokemon?.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(pokemon))
                    return;

                foreach (var kvp in Pokemons)
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
                foreach (var kvp in PokemonAbilities)
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