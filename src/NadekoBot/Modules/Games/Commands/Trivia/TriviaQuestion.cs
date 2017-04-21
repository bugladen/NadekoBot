using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// THANKS @ShoMinamimoto for suggestions and coding help
namespace NadekoBot.Modules.Games.Trivia
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

        public string Category { get; set; }
        public string Question { get; set; }
        public string ImageUrl { get; set; }
        public string AnswerImageUrl { get; set; }
        public string Answer { get; set; }

        public TriviaQuestion(string q, string a, string c, string img = null, string answerImage = null)
        {
            this.Question = q;
            this.Answer = a;
            this.Category = c;
            this.ImageUrl = img;
            this.AnswerImageUrl = answerImage ?? img;
        }

        public string GetHint() => Scramble(Answer);

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

        private static string Scramble(string word)
        {
            var letters = word.ToCharArray();
            var count = 0;
            for (var i = 0; i < letters.Length; i++)
            {
                if (letters[i] == ' ')
                    continue;

                count++;
                if (count <= letters.Length / 5)
                    continue;

                if (count % 3 == 0)
                    continue;

                if (letters[i] != ' ')
                    letters[i] = '_';
            }
            return string.Join(" ", new string(letters).Replace(" ", " \u2000").AsEnumerable());
        }
    }
}
