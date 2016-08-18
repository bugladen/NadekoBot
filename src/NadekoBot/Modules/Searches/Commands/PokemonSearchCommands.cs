using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Modules.Searches.Commands.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches.Commands
{
    public partial class SearchesModule : DiscordModule
    {
        [Group]
        public class PokemonSearchCommands
        {
            //todo DB
            private static Dictionary<string, SearchPokemon> pokemons;
            private static Dictionary<string, SearchPokemonAbility> pokemonAbilities;

            public PokemonSearchCommands()
            {
                pokemons = JsonConvert.DeserializeObject<Dictionary<string, SearchPokemon>>(File.ReadAllText("data/pokemon/pokemon_list.json"));
                pokemonAbilities = JsonConvert.DeserializeObject<Dictionary<string, SearchPokemonAbility>>(File.ReadAllText("data/pokemon/pokemon_abilities.json"));
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task Pokemon(IMessage imsg, [Remainder] string pokemon = null)
            {
                var channel = imsg.Channel as ITextChannel;

                pokemon = pokemon?.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(pokemon))
                    return;

                foreach (var kvp in pokemons)
                {
                    if (kvp.Key.ToUpperInvariant() == pokemon.ToUpperInvariant())
                    {
                        await imsg.Channel.SendMessageAsync($"`Stats for \"{kvp.Key}\" pokemon:`\n{kvp.Value}");
                        return;
                    }
                }
                await imsg.Channel.SendMessageAsync("`No pokemon found.`");
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task PokemonAbility(IMessage imsg, [Remainder] string ability = null)
            {
                var channel = imsg.Channel as ITextChannel;

                ability = ability?.Trim().ToUpperInvariant().Replace(" ", "");
                if (string.IsNullOrWhiteSpace(ability))
                    return;
                foreach (var kvp in pokemonAbilities)
                {
                    if (kvp.Key.ToUpperInvariant() == ability)
                    {
                        await imsg.Channel.SendMessageAsync($"`Info for \"{kvp.Key}\" ability:`\n{kvp.Value}");
                        return;
                    }
                }
                await imsg.Channel.SendMessageAsync("`No ability found.`");
            }
        }
    }
}
