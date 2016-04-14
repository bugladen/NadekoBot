using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// THANKS @ShoMinamimoto for suggestions and coding help
namespace NadekoBot.Modules.Games.Commands.Trivia
{
    public class TriviaQuestion
    {
        //represents the min size to judge levDistance with
        private static readonly HashSet<Tuple<int, int>> strictness = new HashSet<Tuple<int, int>> {
            new Tuple<int, int>(9, 0),
            new Tuple<int, int>(14, 1),
            new Tuple<int, int>(19, 2),
            new Tuple<int, int>(22, 3),
        };
        public static int maxStringLength = 22;

        public string Category;
        public string Question;
        public string Answer;

        public TriviaQuestion(string q, string a, string c)
        {
            this.Question = q;
            this.Answer = a;
            this.Category = c;
        }

        public string GetHint() => Answer.Scramble();

        public bool IsAnswerCorrect(string guess)
        {
            guess = CleanGuess(guess);
            if (Answer.Equals(guess))
            {
                return true;
            }
            Answer = CleanGuess(Answer);
            guess = CleanGuess(guess);
            if (Answer.Equals(guess))
            {
                return true;
            }

            int levDistance = Answer.LevenshteinDistance(guess);
            return JudgeGuess(Answer.Length, guess.Length, levDistance);
        }

        private bool JudgeGuess(int guessLength, int answerLength, int levDistance)
        {
            foreach (Tuple<int, int> level in strictness)
            {
                if (guessLength <= level.Item1 || answerLength <= level.Item1)
                {
                    if (levDistance <= level.Item2)
                        return true;
                    else
                        return false;
                }
            }
            return false;
        }

        private string CleanGuess(string str)
        {
            str = " " + str.ToLower() + " ";
            str = Regex.Replace(str, "\\s+", " ");
            str = Regex.Replace(str, "[^\\w\\d\\s]", "");
            //Here's where custom modification can be done
            str = Regex.Replace(str, "\\s(a|an|the|of|in|for|to|as|at|be)\\s", " ");
            //End custom mod and cleanup whitespace
            str = Regex.Replace(str, "^\\s+", "");
            str = Regex.Replace(str, "\\s+$", "");
            //Trim the really long answers
            str = str.Length <= maxStringLength ? str : str.Substring(0, maxStringLength);
            return str;
        }

        public override string ToString() =>
            "Question: **" + this.Question + "?**";
    }
}
