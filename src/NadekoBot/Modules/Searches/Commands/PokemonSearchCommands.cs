using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Modules.Searches.Models;
using Newtonsoft.Json;
using NLog;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class PokemonSearchCommands
        {
            private static Dictionary<string, SearchPokemon> pokemons = new Dictionary<string, SearchPokemon>();
            private static Dictionary<string, SearchPokemonAbility> pokemonAbilities = new Dictionary<string, SearchPokemonAbility>();

            public const string PokemonAbilitiesFile = "data/pokemon/pokemon_abilities.json";

            public const string PokemonListFile = "data/pokemon/pokemon_list.json";
            private Logger _log;

            public PokemonSearchCommands()
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
            [RequireContext(ContextType.Guild)]
            public async Task Pokemon(IUserMessage umsg, [Remainder] string pokemon = null)
            {
                var channel = (ITextChannel)umsg.Channel;

                pokemon = pokemon?.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(pokemon))
                    return;

                foreach (var kvp in pokemons)
                {
                    if (kvp.Key.ToUpperInvariant() == pokemon.ToUpperInvariant())
                    {
                        await channel.SendMessageAsync($"`Stats for \"{kvp.Key}\" pokemon:`\n{kvp.Value}");
                        return;
                    }
                }
                await channel.SendMessageAsync("`No pokemon found.`");
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task PokemonAbility(IUserMessage umsg, [Remainder] string ability = null)
            {
                var channel = (ITextChannel)umsg.Channel;

                ability = ability?.Trim().ToUpperInvariant().Replace(" ", "");
                if (string.IsNullOrWhiteSpace(ability))
                    return;
                foreach (var kvp in pokemonAbilities)
                {
                    if (kvp.Key.ToUpperInvariant() == ability)
                    {
                        await channel.SendMessageAsync($"`Info for \"{kvp.Key}\" ability:`\n{kvp.Value}");
                        return;
                    }
                }
                await channel.SendMessageAsync("`No ability found.`");
            }
        }
    }
}