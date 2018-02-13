using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Services;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

// taken from 
// http://www.codeproject.com/Tips/207582/L-t-Tr-nsl-t-r-Leet-Translator (thanks)
// because i don't want to waste my time on this cancerous command
//* |-|0\/\/ Ð4®3 ¥0µ!?
namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class LeetCommands : NadekoSubmodule<GamesService>
        {
            [NadekoCommand, Usage, Description, Aliases]
            public async Task Leet(int level, [Remainder] string text = null)
            {
                if (string.IsNullOrWhiteSpace(text = text?.Trim()))
                    return;
                await Context.Channel.SendConfirmAsync("L33t", ToLeet(text, level).SanitizeMentions()).ConfigureAwait(false);
            }

            /// <summary>
            /// Translate text to Leet - Extension methods for string class
            /// </summary>
            /// <param name="text">Orginal text</param>
            /// <param name="degree">Degree of translation (1 - 6)</param>
            /// <returns>Leet translated text</returns>
            private static string ToLeet(string text, int degree = 1) =>
                Translate(text, degree);

            /// <summary>
            /// Translate text to Leet
            /// </summary>
            /// <param name="text">Orginal text</param>
            /// <param name="degree">Degree of translation (1 - 6)</param>
            /// <returns>Leet translated text</returns>
            private static string Translate(string text, int degree = 1)
            {
                if (degree < 1)
                    return text;
                if (degree > 6)
                    degree = 6;

                var degrees = new List<char[]>
                {
                    new[] { 'a', 'e', 'i', 'o', },
                    new[] { 's', 'l', 'c', 'y', 'u', 'd', },
                    new[] { 'k', 'g', 't', 'z', 'f', },
                    new[] { 'n', 'w', 'h', 'v', 'm', },
                    new[] { 'r', 'b', 'q', 'x' },
                    new[] { 'j', 'p' }
                };

                var validChars = new List<char>(degrees[0]);
                for (var i = 1; i < degree; i++)
                    validChars.AddRange(degrees[i]);

                var sb = new StringBuilder(text.Length);
                foreach (char c in text)
                {
                    var letter = char.ToLower(c);

                    if (validChars.Contains(letter))
                        sb.Append(leetLookoup[letter]);
                    else
                        sb.Append(c);
                }

                return sb.ToString().TrimTo(1995);
            }

            private static readonly SortedList<char, string> leetLookoup = new SortedList<char, string>
            {
                ['a'] = @"4",
                ['b'] = @"ß",
                ['c'] = @"(",
                ['d'] = @"Ð",
                ['e'] = @"3",
                ['f'] = @"ƒ",
                ['g'] = @"9",
                ['h'] = @"|-|",
                ['i'] = @"1",
                ['j'] = @"_|",
                ['k'] = @"|{",
                ['l'] = @"£",
                ['m'] = @"|\/|",
                ['n'] = @"|\|",
                ['o'] = @"0",
                ['p'] = @"|°",
                ['q'] = @"¶",
                ['r'] = @"®",
                ['s'] = @"$",
                ['t'] = @"7",
                ['u'] = @"µ",
                ['v'] = @"\/",
                ['w'] = @"\/\/",
                ['x'] = @"(",
                ['y'] = @"¥",
                ['z'] = @"2"
            };
        }
    }
}
