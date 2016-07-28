using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Extensions
{
    public static class Extensions
    {
        private static Random rng = new Random();

        public static string Scramble(this string word)
        {

            var letters = word.ToArray();
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
            return "`" + string.Join(" ", letters) + "`";
        }
        public static string TrimTo(this string str, int num, bool hideDots = false)
        {
            if (num < 0)
                throw new ArgumentOutOfRangeException(nameof(num), "TrimTo argument cannot be less than 0");
            if (num == 0)
                return string.Empty;
            if (num <= 3)
                return string.Concat(str.Select(c => '.'));
            if (str.Length < num)
                return str;
            return string.Concat(str.Take(num - 3)) + (hideDots ? "" : "...");
        }
        /// <summary>
        /// Removes trailing S or ES (if specified) on the given string if the num is 1
        /// </summary>
        /// <param name="str"></param>
        /// <param name="num"></param>
        /// <param name="es"></param>
        /// <returns>String with the correct singular/plural form</returns>
        public static string SnPl(this string str, int? num, bool es = false)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));
            if (num == null)
                throw new ArgumentNullException(nameof(num));
            return num == 1 ? str.Remove(str.Length - 1, es ? 2 : 1) : str;
        }

        /// <summary>
        /// Sends a message to the channel from which this command is called.
        /// </summary>
        /// <param name="e">EventArg</param>
        /// <param name="message">Message to be sent</param>
        /// <returns></returns>
        public static async Task<Message> Send(this CommandEventArgs e, string message)
            => await e.Channel.SendMessage(message).ConfigureAwait(false);

        /// <summary>
        /// Sends a message to the channel from which MessageEventArg came.
        /// </summary>
        /// <param name="e">EventArg</param>
        /// <param name="message">Message to be sent</param>
        /// <returns></returns>
        public static async Task Send(this MessageEventArgs e, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            await e.Channel.SendMessage(message).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a message to this channel.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Send(this Channel c, string message)
        {
            await c.SendMessage(message).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a private message to this user.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Send(this User u, string message)
        {
            await u.SendMessage(message).ConfigureAwait(false);
        }

        /// <summary>
        /// Replies to a user who invoked this command, message start with that user's mention.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Reply(this CommandEventArgs e, string message)
        {
            await e.Channel.SendMessage(e.User.Mention + " " + message).ConfigureAwait(false);
        }

        /// <summary>
        /// Replies to a user who invoked this command, message start with that user's mention.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Reply(this MessageEventArgs e, string message)
        {
            await e.Channel.SendMessage(e.User.Mention + " " + message).ConfigureAwait(false);
        }

        /// <summary>
        /// Randomizes element order in a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static IList<T> Shuffle<T>(this IList<T> list)
        {

            // Thanks to @Joe4Evr for finding a bug in the old version of the shuffle
            var provider = new RNGCryptoServiceProvider();
            var n = list.Count;
            while (n > 1)
            {
                var box = new byte[(n / Byte.MaxValue) + 1];
                int boxSum;
                do
                {
                    provider.GetBytes(box);
                    boxSum = box.Sum(b => b);
                }
                while (!(boxSum < n * ((Byte.MaxValue * box.Length) / n)));
                var k = (boxSum % n);
                n--;
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }

        /// <summary>
        /// Shortens a string URL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="action"></param>
        public static async Task<string> ShortenUrl(this string str)
        {
            try
            {
                var result = await SearchHelper.ShortenUrl(str).ConfigureAwait(false);
                return result;
            }
            catch (WebException ex)
            {
                throw new InvalidOperationException("You must enable URL shortner in google developers console.", ex);
            }
        }

        public static string GetOnPage<T>(this IEnumerable<T> source, int pageIndex, int itemsPerPage = 5)
        {
            var items = source.Skip(pageIndex * itemsPerPage).Take(itemsPerPage);
            if (!items.Any())
            {
                return $"No items on page {pageIndex + 1}.";
            }
            var sb = new StringBuilder($"---page {pageIndex + 1} --\n");
            var itemsDC = items as IEnumerable<KeyValuePair<string, IEnumerable<string>>>;
            var itemsDS = items as IEnumerable<KeyValuePair<string, string>>;
            if (itemsDC != null)
            {
                foreach (var item in itemsDC)
                {
                    sb.Append($"{ Format.Code(item.Key)}\n");
                    int i = 1;
                    var last = item.Value.Last();
                    foreach (var value in item.Value)
                    {
                        if (last != value)
                            sb.AppendLine("  `├" + i++ + "─`" + Format.Bold(value));
                        else
                            sb.AppendLine("  `└" + i++ + "─`" + Format.Bold(value));
                    }

                }
            }
            else if (itemsDS != null)
            {
                foreach (var item in itemsDS)
                {
                    sb.Append($"{ Format.Code(item.Key)}\n");
                    sb.AppendLine("  `└─`" + Format.Bold(item.Value));
                }

            }
            else
            {
                foreach (var item in items)
                {
                    sb.Append($"{ Format.Code(item.ToString())} \n");
                }
            }

            return sb.ToString();
        }
        /// <summary>
        /// Gets the program runtime
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="action"></param>
        public static string GetRuntime(this DiscordClient c) => ".Net Framework 4.5.2";

        public static string Matrix(this string s)
            =>
                string.Concat(s.Select(c => c.ToString() + " ̵̢̬̜͉̞̭̖̰͋̉̎ͬ̔̇̌̀".TrimTo(rng.Next(0, 12), true)));
        //.Replace("`", "");

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source)
            {
                action(element);
            }
        }

        //http://www.dotnetperls.com/levenshtein
        public static int LevenshteinDistance(this string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (var i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (var j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (var i = 1; i <= n; i++)
            {
                //Step 4
                for (var j = 1; j <= m; j++)
                {
                    // Step 5
                    var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        public static int KiB(this int value) => value * 1024;
        public static int KB(this int value) => value * 1000;

        public static int MiB(this int value) => value.KiB() * 1024;
        public static int MB(this int value) => value.KB() * 1000;

        public static int GiB(this int value) => value.MiB() * 1024;
        public static int GB(this int value) => value.MB() * 1000;

        public static ulong KiB(this ulong value) => value * 1024;
        public static ulong KB(this ulong value) => value * 1000;

        public static ulong MiB(this ulong value) => value.KiB() * 1024;
        public static ulong MB(this ulong value) => value.KB() * 1000;

        public static ulong GiB(this ulong value) => value.MiB() * 1024;
        public static ulong GB(this ulong value) => value.MB() * 1000;

        public static Stream ToStream(this Image img, System.Drawing.Imaging.ImageFormat format = null)
        {
            if (format == null)
                format = System.Drawing.Imaging.ImageFormat.Jpeg;
            var stream = new MemoryStream();
            img.Save(stream, format);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Merges Images into 1 Image and returns a bitmap.
        /// </summary>
        /// <param name="images">The Images you want to merge.</param>
        /// <returns>Merged bitmap</returns>
        public static Bitmap Merge(this IEnumerable<Image> images, int reverseScaleFactor = 1)
        {
            var imageArray = images as Image[] ?? images.ToArray();
            if (!imageArray.Any()) return null;
            var width = imageArray.Sum(i => i.Width);
            var height = imageArray.First().Height;
            var bitmap = new Bitmap(width / reverseScaleFactor, height / reverseScaleFactor);
            var r = new Random();
            var offsetx = 0;
            foreach (var img in imageArray)
            {
                var bm = new Bitmap(img);
                for (var w = 0; w < img.Width; w++)
                {
                    for (var h = 0; h < bitmap.Height; h++)
                    {
                        bitmap.SetPixel(w / reverseScaleFactor + offsetx, h, bm.GetPixel(w, h * reverseScaleFactor));
                    }
                }
                offsetx += img.Width / reverseScaleFactor;
            }
            return bitmap;
        }

        /// <summary>
        /// Merges Images into 1 Image and returns a bitmap asynchronously.
        /// </summary>
        /// <param name="images">The Images you want to merge.</param>
        /// <param name="reverseScaleFactor"></param>
        /// <returns>Merged bitmap</returns>
        public static async Task<Bitmap> MergeAsync(this IEnumerable<Image> images, int reverseScaleFactor = 1) =>
            await Task.Run(() => images.Merge(reverseScaleFactor)).ConfigureAwait(false);

        public static string Unmention(this string str) => str.Replace("@", "ම");

        public static Stream ToStream(this string str)
        {
            var sw = new StreamWriter(new MemoryStream());
            sw.Write(str);
            sw.Flush();
            sw.BaseStream.Position = 0;
            return sw.BaseStream;
        }

        public static double UnixTimestamp(this DateTime dt) => dt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

    }
}
