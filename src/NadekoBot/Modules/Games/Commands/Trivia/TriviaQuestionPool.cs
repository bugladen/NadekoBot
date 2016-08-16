using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NadekoBot.Modules.Games.Commands.Trivia
{
    public class TriviaQuestionPool
    {
        public static TriviaQuestionPool Instance { get; } = new TriviaQuestionPool();

        public HashSet<TriviaQuestion> pool = new HashSet<TriviaQuestion>();

        private Random rng { get; } = new Random();

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

        internal void Reload()
        {
            var arr = JArray.Parse(File.ReadAllText("data/questions.json"));

            foreach (var item in arr)
            {
                var tq = new TriviaQuestion(item["Question"].ToString(), item["Answer"].ToString(), item["Category"]?.ToString());
                pool.Add(tq);
            }
            var r = new Random();
            pool = new HashSet<TriviaQuestion>(pool.OrderBy(x => r.Next()));
        }
    }
}
