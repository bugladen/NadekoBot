using NadekoBot.Extensions;
using NadekoBot.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NadekoBot.Modules.Games.Trivia
{
    public class TriviaQuestionPool
    {
        private static TriviaQuestionPool _instance;
        public static TriviaQuestionPool Instance { get; } = _instance ?? (_instance = new TriviaQuestionPool());

        private const string questionsFile = "data/trivia_questions.json";

        private Random rng { get; } = new NadekoRandom();
        
        private TriviaQuestion[] pool { get; }

        static TriviaQuestionPool() { }

        private TriviaQuestionPool()
        {
            pool = JsonConvert.DeserializeObject<TriviaQuestion[]>(File.ReadAllText(questionsFile));
        }

        public TriviaQuestion GetRandomQuestion(HashSet<TriviaQuestion> exclude)
        {
            if (pool.Length == 0)
                return null;

            TriviaQuestion randomQuestion;
            while (exclude.Contains(randomQuestion = pool[rng.Next(0, pool.Length)])) ;

            return randomQuestion;
        }
    }
}
