using Newtonsoft.Json;
using NLog;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace NadekoBot.Services.Pokemon
{
    public class PokemonService
    {
        public readonly List<PokemonType> PokemonTypes = new List<PokemonType>();
        public readonly ConcurrentDictionary<ulong, PokeStats> Stats = new ConcurrentDictionary<ulong, PokeStats>();

        public const string PokemonTypesFile = "data/pokemon_types.json";

        private Logger _log { get; }

        public PokemonService()
        {
            _log = LogManager.GetCurrentClassLogger();
            if (File.Exists(PokemonTypesFile))
            {
                PokemonTypes = JsonConvert.DeserializeObject<List<PokemonType>>(File.ReadAllText(PokemonTypesFile));
            }
            else
            {
                PokemonTypes = new List<PokemonType>();
                _log.Warn(PokemonTypesFile + " is missing. Pokemon types not loaded.");
            }
        }
    }
}
