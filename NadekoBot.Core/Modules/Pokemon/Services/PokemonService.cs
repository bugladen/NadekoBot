using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using NadekoBot.Modules.Pokemon.Common;
using NadekoBot.Core.Services;
using Newtonsoft.Json;
using NLog;

namespace NadekoBot.Modules.Pokemon.Services
{
    public class PokemonService : INService
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
