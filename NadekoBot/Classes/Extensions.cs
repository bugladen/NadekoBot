using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace NadekoBot
{
    public static class Extensions
    {
        public static string Scramble(this string word) {

            var letters = word.ToArray();
            for (int i = 0; i < letters.Length; i++)
            {
                if (i % 3 == 0)
                {
                    continue;
                }

                if (letters[i] != ' ')
                    letters[i] = '_';
            }
            return "`"+string.Join(" ", letters)+"`";
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
