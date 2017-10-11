using NadekoBot.Common;
using NadekoBot.Modules.Games.Common.Hangman.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NadekoBot.Modules.Games.Common.Hangman
{
    public class TermPool
    {
        const string termsPath = "data/hangman3.json";
        public static IReadOnlyDictionary<string, HangmanObject[]> Data { get; } = new Dictionary<string, HangmanObject[]>();
        static TermPool()
        {
            try
            {
                Data = JsonConvert.DeserializeObject<Dictionary<string, HangmanObject[]>>(File.ReadAllText(termsPath));
            }
            catch (Exception)
            {
                //ignored
            }
        }

        public static HangmanObject GetTerm(string type)
        {
            var rng = new NadekoRandom();

            if (type == "random")
            {
                var keys = Data.Keys.ToArray();

                type = Data.Keys.ToArray()[rng.Next(0, Data.Keys.Count())];
            }
            if (!Data.TryGetValue(type, out var termTypes) || termTypes.Length == 0)
                throw new TermNotFoundException();

            var obj = termTypes[rng.Next(0, termTypes.Length)];

            obj.Word = obj.Word.Trim().ToLowerInvariant();
            return obj;
        }
    }
}
