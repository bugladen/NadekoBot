using Discord.Commands;
using NadekoBot.Extensions;
using System.Text;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;

// taken from 
// http://www.codeproject.com/Tips/207582/L-t-Tr-nsl-t-r-Leet-Translator (thanks)
// because i don't want to waste my time on this cancerous command
namespace NadekoBot.Modules.Games
{
    public partial class Games
    {
        [NadekoCommand, Usage, Description, Aliases]
        public async Task Leet(int level, [Remainder] string text = null)
        {
            text = text.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return;
            await Context.Channel.SendConfirmAsync("L33t", ToLeet(text, level).SanitizeMentions()).ConfigureAwait(false);
        }


        /// <summary>
        /// Translate text to Leet - Extension methods for string class
        /// </summary>
        /// <param name="text">Orginal text</param>
        /// <param name="degree">Degree of translation (1 - 3)</param>
        /// <returns>Leet translated text</returns>
        private static string ToLeet(string text, int degree = 1) =>
            Translate(text, degree);

        /// <summary>
        /// Translate text to Leet
        /// </summary>
        /// <param name="text">Orginal text</param>
        /// <param name="degree">Degree of translation (1 - 3)</param>
        /// <returns>Leet translated text</returns>
        private static string Translate(string text, int degree = 1)
        {
            if (degree > 6)
                degree = 6;
            if (degree <= 0)
                return text;

            // StringBuilder to store result.
            StringBuilder sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                #region Degree 1
                if (degree == 1)
                {
                    switch (c)
                    {
                        case 'a': sb.Append("4"); break;
                        case 'e': sb.Append("3"); break;
                        case 'i': sb.Append("1"); break;
                        case 'o': sb.Append("0"); break;
                        case 'A': sb.Append("4"); break;
                        case 'E': sb.Append("3"); break;
                        case 'I': sb.Append("1"); break;
                        case 'O': sb.Append("0"); break;
                        default: sb.Append(c); break;
                    }
                }
                #endregion
                #region Degree 2
                else if (degree == 2)
                {
                    switch (c)
                    {
                        case 'a': sb.Append("4"); break;
                        case 'e': sb.Append("3"); break;
                        case 'i': sb.Append("1"); break;
                        case 'o': sb.Append("0"); break;
                        case 'A': sb.Append("4"); break;
                        case 'E': sb.Append("3"); break;
                        case 'I': sb.Append("1"); break;
                        case 'O': sb.Append("0"); break;
                        case 's': sb.Append("$"); break;
                        case 'S': sb.Append("$"); break;
                        case 'l': sb.Append("£"); break;
                        case 'L': sb.Append("£"); break;
                        case 'c': sb.Append("("); break;
                        case 'C': sb.Append("("); break;
                        case 'y': sb.Append("¥"); break;
                        case 'Y': sb.Append("¥"); break;
                        case 'u': sb.Append("µ"); break;
                        case 'U': sb.Append("µ"); break;
                        case 'd': sb.Append("Ð"); break;
                        case 'D': sb.Append("Ð"); break;
                        default: sb.Append(c); break;
                    }
                }
                #endregion
                #region Degree 3
                else if (degree == 3)
                {
                    switch (c)
                    {
                        case 'a': sb.Append("4"); break;
                        case 'e': sb.Append("3"); break;
                        case 'i': sb.Append("1"); break;
                        case 'o': sb.Append("0"); break;
                        case 'A': sb.Append("4"); break;
                        case 'E': sb.Append("3"); break;
                        case 'I': sb.Append("1"); break;
                        case 'O': sb.Append("0"); break;
                        case 'k': sb.Append("|{"); break;
                        case 'K': sb.Append("|{"); break;
                        case 's': sb.Append("$"); break;
                        case 'S': sb.Append("$"); break;
                        case 'g': sb.Append("9"); break;
                        case 'G': sb.Append("9"); break;
                        case 'l': sb.Append("£"); break;
                        case 'L': sb.Append("£"); break;
                        case 'c': sb.Append("("); break;
                        case 'C': sb.Append("("); break;
                        case 't': sb.Append("7"); break;
                        case 'T': sb.Append("7"); break;
                        case 'z': sb.Append("2"); break;
                        case 'Z': sb.Append("2"); break;
                        case 'y': sb.Append("¥"); break;
                        case 'Y': sb.Append("¥"); break;
                        case 'u': sb.Append("µ"); break;
                        case 'U': sb.Append("µ"); break;
                        case 'f': sb.Append("ƒ"); break;
                        case 'F': sb.Append("ƒ"); break;
                        case 'd': sb.Append("Ð"); break;
                        case 'D': sb.Append("Ð"); break;
                        default: sb.Append(c); break;
                    }
                }
                #endregion
                #region Degree 4
                else if (degree == 4)
                {
                    switch (c)
                    {
                        case 'a': sb.Append("4"); break;
                        case 'e': sb.Append("3"); break;
                        case 'i': sb.Append("1"); break;
                        case 'o': sb.Append("0"); break;
                        case 'A': sb.Append("4"); break;
                        case 'E': sb.Append("3"); break;
                        case 'I': sb.Append("1"); break;
                        case 'O': sb.Append("0"); break;
                        case 'k': sb.Append("|{"); break;
                        case 'K': sb.Append("|{"); break;
                        case 's': sb.Append("$"); break;
                        case 'S': sb.Append("$"); break;
                        case 'g': sb.Append("9"); break;
                        case 'G': sb.Append("9"); break;
                        case 'l': sb.Append("£"); break;
                        case 'L': sb.Append("£"); break;
                        case 'c': sb.Append("("); break;
                        case 'C': sb.Append("("); break;
                        case 't': sb.Append("7"); break;
                        case 'T': sb.Append("7"); break;
                        case 'z': sb.Append("2"); break;
                        case 'Z': sb.Append("2"); break;
                        case 'y': sb.Append("¥"); break;
                        case 'Y': sb.Append("¥"); break;
                        case 'u': sb.Append("µ"); break;
                        case 'U': sb.Append("µ"); break;
                        case 'f': sb.Append("ƒ"); break;
                        case 'F': sb.Append("ƒ"); break;
                        case 'd': sb.Append("Ð"); break;
                        case 'D': sb.Append("Ð"); break;
                        case 'n': sb.Append(@"|\\|"); break;
                        case 'N': sb.Append(@"|\\|"); break;
                        case 'w': sb.Append(@"\\/\\/"); break;
                        case 'W': sb.Append(@"\\/\\/"); break;
                        case 'h': sb.Append(@"|-|"); break;
                        case 'H': sb.Append(@"|-|"); break;
                        case 'v': sb.Append(@"\\/"); break;
                        case 'V': sb.Append(@"\\/"); break;
                        case 'm': sb.Append(@"|\\/|"); break;
                        case 'M': sb.Append(@"|\/|"); break;
                        default: sb.Append(c); break;
                    }
                }
                #endregion
                #region Degree 5
                else if (degree == 5)
                {
                    switch (c)
                    {
                        case 'a': sb.Append("4"); break;
                        case 'e': sb.Append("3"); break;
                        case 'i': sb.Append("1"); break;
                        case 'o': sb.Append("0"); break;
                        case 'A': sb.Append("4"); break;
                        case 'E': sb.Append("3"); break;
                        case 'I': sb.Append("1"); break;
                        case 'O': sb.Append("0"); break;
                        case 's': sb.Append("$"); break;
                        case 'S': sb.Append("$"); break;
                        case 'g': sb.Append("9"); break;
                        case 'G': sb.Append("9"); break;
                        case 'l': sb.Append("£"); break;
                        case 'L': sb.Append("£"); break;
                        case 'c': sb.Append("("); break;
                        case 'C': sb.Append("("); break;
                        case 't': sb.Append("7"); break;
                        case 'T': sb.Append("7"); break;
                        case 'z': sb.Append("2"); break;
                        case 'Z': sb.Append("2"); break;
                        case 'y': sb.Append("¥"); break;
                        case 'Y': sb.Append("¥"); break;
                        case 'u': sb.Append("µ"); break;
                        case 'U': sb.Append("µ"); break;
                        case 'f': sb.Append("ƒ"); break;
                        case 'F': sb.Append("ƒ"); break;
                        case 'd': sb.Append("Ð"); break;
                        case 'D': sb.Append("Ð"); break;
                        case 'n': sb.Append(@"|\\|"); break;
                        case 'N': sb.Append(@"|\\|"); break;
                        case 'w': sb.Append(@"\\/\\/"); break;
                        case 'W': sb.Append(@"\\/\\/"); break;
                        case 'h': sb.Append("|-|"); break;
                        case 'H': sb.Append("|-|"); break;
                        case 'v': sb.Append("\\/"); break;
                        case 'V': sb.Append(@"\\/"); break;
                        case 'k': sb.Append("|{"); break;
                        case 'K': sb.Append("|{"); break;
                        case 'r': sb.Append("®"); break;
                        case 'R': sb.Append("®"); break;
                        case 'm': sb.Append(@"|\\/|"); break;
                        case 'M': sb.Append(@"|\\/|"); break;
                        case 'b': sb.Append("ß"); break;
                        case 'B': sb.Append("ß"); break;
                        case 'q': sb.Append("Q"); break;
                        case 'Q': sb.Append("Q¸"); break;
                        case 'x': sb.Append(")("); break;
                        case 'X': sb.Append(")("); break;
                        default: sb.Append(c); break;
                    }
                }
                #endregion
                #region Degree 6
                else if (degree == 6)
                {
                    switch (c)
                    {
                        case 'a': sb.Append("4"); break;
                        case 'e': sb.Append("3"); break;
                        case 'i': sb.Append("1"); break;
                        case 'o': sb.Append("0"); break;
                        case 'A': sb.Append("4"); break;
                        case 'E': sb.Append("3"); break;
                        case 'I': sb.Append("1"); break;
                        case 'O': sb.Append("0"); break;
                        case 's': sb.Append("$"); break;
                        case 'S': sb.Append("$"); break;
                        case 'g': sb.Append("9"); break;
                        case 'G': sb.Append("9"); break;
                        case 'l': sb.Append("£"); break;
                        case 'L': sb.Append("£"); break;
                        case 'c': sb.Append("("); break;
                        case 'C': sb.Append("("); break;
                        case 't': sb.Append("7"); break;
                        case 'T': sb.Append("7"); break;
                        case 'z': sb.Append("2"); break;
                        case 'Z': sb.Append("2"); break;
                        case 'y': sb.Append("¥"); break;
                        case 'Y': sb.Append("¥"); break;
                        case 'u': sb.Append("µ"); break;
                        case 'U': sb.Append("µ"); break;
                        case 'f': sb.Append("ƒ"); break;
                        case 'F': sb.Append("ƒ"); break;
                        case 'd': sb.Append("Ð"); break;
                        case 'D': sb.Append("Ð"); break;
                        case 'n': sb.Append(@"|\\|"); break;
                        case 'N': sb.Append(@"|\\|"); break;
                        case 'w': sb.Append(@"\\/\\/"); break;
                        case 'W': sb.Append(@"\\/\\/"); break;
                        case 'h': sb.Append("|-|"); break;
                        case 'H': sb.Append("|-|"); break;
                        case 'v': sb.Append(@"\\/"); break;
                        case 'V': sb.Append(@"\\/"); break;
                        case 'k': sb.Append("|{"); break;
                        case 'K': sb.Append("|{"); break;
                        case 'r': sb.Append("®"); break;
                        case 'R': sb.Append("®"); break;
                        case 'm': sb.Append(@"|\\/|"); break;
                        case 'M': sb.Append(@"|\\/|"); break;
                        case 'b': sb.Append("ß"); break;
                        case 'B': sb.Append("ß"); break;
                        case 'j': sb.Append("_|"); break;
                        case 'J': sb.Append("_|"); break;
                        case 'P': sb.Append("|°"); break;
                        case 'q': sb.Append("¶"); break;
                        case 'Q': sb.Append("¶¸"); break;
                        case 'x': sb.Append(")("); break;
                        case 'X': sb.Append(")("); break;
                        default: sb.Append(c); break;
                    }
                }
                #endregion
            }
            return sb.ToString().TrimTo(1995); // Return result.
        }
    }
}
