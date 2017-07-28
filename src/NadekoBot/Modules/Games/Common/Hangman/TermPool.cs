using NadekoBot.Common;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Common.Hangman.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace NadekoBot.Modules.Games.Common.Hangman
{
    public class TermPool
    {
        const string termsPath = "data/hangman3.json";
        public static IReadOnlyDictionary<string, HangmanObject[]> data { get; } = new Dictionary<string, HangmanObject[]>();
        static TermPool()
        {
            try
            {
                data = JsonConvert.DeserializeObject<Dictionary<string, HangmanObject[]>>(File.ReadAllText(termsPath));
            }
            catch (Exception)
            {
                //ignored
            }
        }

        private static readonly ImmutableArray<TermType> _termTypes = Enum.GetValues(typeof(TermType))
                                                                            .Cast<TermType>()
                                                                            .ToImmutableArray();

        public static HangmanObject GetTerm(TermType type)
        {
            var rng = new NadekoRandom();

            if (type == TermType.Random)
            {
                var keys = data.Keys.ToArray();
                
                type = _termTypes[rng.Next(0, _termTypes.Length - 1)]; // - 1 because last one is 'all'
            }
            if (!data.TryGetValue(type.ToString(), out var termTypes) || termTypes.Length == 0)
                throw new TermNotFoundException();

            var obj = termTypes[rng.Next(0, termTypes.Length)];

            obj.Word = obj.Word.Trim().ToLowerInvariant();
            return obj;
        }
    }
}
