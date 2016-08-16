using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Extensions
{
    public static class Extensions
    {
        public static async Task<IMessage> Reply(this IMessage msg, string content) => await msg.Channel.SendMessageAsync(content);

        public static async Task<IMessage[]> ReplyLong(this IMessage msg, string content, string breakOn = "\n", string addToEnd = "", string addToStart = "")
        {

            if (content.Length < 2000) return new[] { await msg.Channel.SendMessageAsync(content) };
            var list = new List<IMessage>();

            var temp = Regex.Split(content, breakOn).Select(x => x += breakOn).ToList();
            string toolong;
            //while ((toolong = temp.FirstOrDefault(x => x.Length > 2000)) != null)
            //{
            //    more desperate measures == split on whitespace?
            //}

            StringBuilder builder = new StringBuilder();
            //make this less crappy to look at, maybe it's bugged
            for (int i = 0; i < temp.Count; i++)
            {
                var addition = temp[i];
                //we append 

                if (builder.Length == 0 && i != 0) builder.Append(addToStart + addition);
                else builder.Append(addition);

                //Check if the next would have room
                if (i + 1 >= temp.Count || temp[i + 1].Length + builder.Length + addToEnd.Length > 2000)
                {
                    if (i + 1 < temp.Count) builder.Append(addToEnd);
                    list.Add(await msg.Channel.SendMessageAsync(builder.ToString()));
                    builder.Clear();
                }
            }

            return list.ToArray();
        }

        public static Task<IMessage> SendTableAsync<T>(this IMessageChannel ch, IEnumerable<T> items, Func<T, string> howToPrint, int columns = 3)
        {
            var i = 0;
            return ch.SendMessageAsync($@"```xl
{string.Join("\n", items.GroupBy(item => (i++) / columns)
                        .Select(ig => string.Concat(ig.Select(el => howToPrint(el)))))}
```");
        }

        public static async Task<string> ShortenUrl(this string url)
        {
            if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.GoogleApiKey)) return url;
            try
            {
                var httpWebRequest =
                    (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/urlshortener/v1/url?key=" +
                                                       NadekoBot.Credentials.GoogleApiKey);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(await httpWebRequest.GetRequestStreamAsync().ConfigureAwait(false)))
                {
                    var json = "{\"longUrl\":\"" + Uri.EscapeDataString(url) + "\"}";
                    streamWriter.Write(json);
                }

                var httpResponse = (await httpWebRequest.GetResponseAsync().ConfigureAwait(false)) as HttpWebResponse;
                var responseStream = httpResponse.GetResponseStream();
                using (var streamReader = new StreamReader(responseStream))
                {
                    var responseText = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                    return Regex.Match(responseText, @"""id"": ?""(?<id>.+)""").Groups["id"].Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Shortening of this url failed: " + url);
                Console.WriteLine(ex.ToString());
                return url;
            }
        }

        /// <summary>
        /// returns an IEnumerable with randomized element order
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> items)
        {
            // Thanks to @Joe4Evr for finding a bug in the old version of the shuffle
            using (var provider = RandomNumberGenerator.Create())
            {
                var list = items.ToList();
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
        }

        public static string TrimTo(this string str, int maxLength, bool hideDots = false)
        {
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), $"Argument {nameof(maxLength)} can't be negative.");
            if (maxLength == 0)
                return string.Empty;
            if (maxLength <= 3)
                return string.Concat(str.Select(c => '.'));
            if (str.Length < maxLength)
                return str;
            return string.Concat(str.Take(maxLength - 3)) + (hideDots ? "" : "...");
        }

    }
}