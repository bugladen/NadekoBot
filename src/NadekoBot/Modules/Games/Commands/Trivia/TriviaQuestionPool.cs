using NadekoBot.Extensions;
using NadekoBot.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NadekoBot.Modules.Games.Trivia
{
    public class TriviaQuestionPool
    {
        public static TriviaQuestionPool Instance { get; } = new TriviaQuestionPool();
        public HashSet<TriviaQuestion> pool = new HashSet<TriviaQuestion>();

        private Random rng { get; } = new NadekoRandom();

        static TriviaQuestionPool() { }

        private TriviaQuestionPool()
        {
            Reload();
        }

        public TriviaQuestion GetRandomQuestion(IEnumerable<TriviaQuestion> exclude)
        {
            var list = pool.Except(exclude).ToList();
            var rand = rng.Next(0, list.Count);
            return list[rand];
        }

        public void Reload()
        {
            var arr = JArray.Parse(File.ReadAllText("data/questions.json"));

            foreach (var item in arr)
            {
                var tq = new TriviaQuestion(item["Question"].ToString().SanitizeMentions(), item["Answer"].ToString().SanitizeMentions(), item["Category"]?.ToString());
                pool.Add(tq);
            }
            var r = new NadekoRandom();
            pool = new HashSet<TriviaQuestion>(pool.OrderBy(x => r.Next()));
        }
    }
}
