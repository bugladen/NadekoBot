using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NadekoBot.Extensions;

namespace NadekoBot.Classes.Trivia {
    public class TriviaQuestionPool {
        private static readonly TriviaQuestionPool _instance = new TriviaQuestionPool();
        public static TriviaQuestionPool Instance => _instance;

        public List<TriviaQuestion> pool = new List<TriviaQuestion>();

        private Random _r { get; } = new Random();

        static TriviaQuestionPool() { }

        private TriviaQuestionPool() {
            Reload();
        }

        public TriviaQuestion GetRandomQuestion(List<TriviaQuestion> exclude) {
            var list = pool.Except(exclude).ToList();
            var rand = _r.Next(0, list.Count);
            return list[rand];
        }

        internal void Reload() {
            JArray arr = JArray.Parse(File.ReadAllText("data/questions.txt"));

            foreach (var item in arr) {
                TriviaQuestion tq;
                tq = new TriviaQuestion(item["Question"].ToString(), item["Answer"].ToString(), item["Category"]?.ToString());
                pool.Add(tq);
            }
            var r = new Random();
            pool = pool.OrderBy(x => r.Next()).ToList();
        }
    }
}
