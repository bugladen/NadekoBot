using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
            //    //TODO more desperate measures == split on whitespace?
            //}

            StringBuilder builder = new StringBuilder();
            //TODO make this less crappy to look at, maybe it's bugged
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

    }
}