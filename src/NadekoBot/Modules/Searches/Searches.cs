using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using NadekoBot.Services;
using System.Threading.Tasks;
using NadekoBot.Attributes;
using System.Text.RegularExpressions;
using System.Net;
using Discord.WebSocket;
using NadekoBot.Modules.Searches.Models;
using NadekoBot.Modules.Searches.IMDB;

namespace NadekoBot.Modules.Searches
{
    [Module("~", AppendSpace = false)]
    public partial class Searches : DiscordModule
    {
        private IGoogleApiService _google { get; }

        public Searches(ILocalization loc, CommandService cmds, IBotConfiguration config, DiscordSocketClient client, IGoogleApiService youtube) : base(loc, cmds, config, client)
        {
            _google = youtube;
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Weather(IMessage imsg, string city, string country)
        {
            var channel = (ITextChannel)imsg.Channel;
            city = city.Replace(" ", "");
            country = city.Replace(" ", "");
            string response;
            using (var http = new HttpClient())
                response = await http.GetStringAsync($"http://api.lawlypopzz.xyz/nadekobot/weather/?city={city}&country={country}").ConfigureAwait(false);

            var obj = JObject.Parse(response)["weather"];

            await channel.SendMessageAsync(
$@"🌍 **Weather for** 【{obj["target"]}】
📏 **Lat,Long:** ({obj["latitude"]}, {obj["longitude"]}) ☁ **Condition:** {obj["condition"]}
😓 **Humidity:** {obj["humidity"]}% 💨 **Wind Speed:** {obj["windspeedk"]}km/h / {obj["windspeedm"]}mph 
🔆 **Temperature:** {obj["centigrade"]}°C / {obj["fahrenheit"]}°F 🔆 **Feels like:** {obj["feelscentigrade"]}°C / {obj["feelsfahrenheit"]}°F
🌄 **Sunrise:** {obj["sunrise"]} 🌇 **Sunset:** {obj["sunset"]}").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Youtube(IMessage imsg, [Remainder] string query = null)
        {
            var channel = (ITextChannel)imsg.Channel;
            if (!(await ValidateQuery(imsg.Channel as ITextChannel, query).ConfigureAwait(false))) return;
            var result = (await _google.GetVideosByKeywordsAsync(query, 1)).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(result))
            {
                await channel.SendMessageAsync("No results found for that query.");
                return;
            }
            await channel.SendMessageAsync(result).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Imdb(IMessage imsg, [Remainder] string query = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            if (!(await ValidateQuery(imsg.Channel as ITextChannel, query).ConfigureAwait(false))) return;
            await imsg.Channel.TriggerTypingAsync().ConfigureAwait(false);
            string result;
            try
            {
                var movie = await ImdbScraper.ImdbScrape(query, true);
                if (movie.Status) result = movie.ToString();
                else result = "Failed to find that movie.";
            }
            catch
            {
                await channel.SendMessageAsync("Failed to find that movie.").ConfigureAwait(false);
                return;
            }

            await channel.SendMessageAsync(result.ToString()).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task RandomCat(IMessage imsg)
        {
            var channel = (ITextChannel)imsg.Channel;
            using (var http = new HttpClient())
            {
                await channel.SendMessageAsync(JObject.Parse(
                                await http.GetStringAsync("http://www.random.cat/meow").ConfigureAwait(false))["file"].ToString())
                                    .ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task RandomDog(IMessage imsg)
        {
            var channel = (ITextChannel)imsg.Channel;
            using (var http = new HttpClient())
            {
                await channel.SendMessageAsync("http://random.dog/" + await http.GetStringAsync("http://random.dog/woof").ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task I(IMessage imsg, [Remainder] string query = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            if (string.IsNullOrWhiteSpace(query))
                return;
            try
            {
                using (var http = new HttpClient())
                {
                    var reqString = $"https://www.googleapis.com/customsearch/v1?q={Uri.EscapeDataString(query)}&cx=018084019232060951019%3Ahs5piey28-e&num=1&searchType=image&fields=items%2Flink&key={NadekoBot.Credentials.GoogleApiKey}";
                    var obj = JObject.Parse(await http.GetStringAsync(reqString).ConfigureAwait(false));
                    await channel.SendMessageAsync(obj["items"][0]["link"].ToString()).ConfigureAwait(false);
                }
            }
            catch (HttpRequestException exception)
            {
                if (exception.Message.Contains("403 (Forbidden)"))
                {
                    await channel.SendMessageAsync("Daily limit reached!");
                }
                else
                {
                    await channel.SendMessageAsync("Something went wrong.");
                }
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Ir(IMessage imsg, [Remainder] string query = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            if (string.IsNullOrWhiteSpace(query))
                return;
            try
            {
                using (var http = new HttpClient())
                {
                    var rng = new Random();
                    var reqString = $"https://www.googleapis.com/customsearch/v1?q={Uri.EscapeDataString(query)}&cx=018084019232060951019%3Ahs5piey28-e&num=1&searchType=image&start={ rng.Next(1, 50) }&fields=items%2Flink&key={NadekoBot.Credentials.GoogleApiKey}";
                    var obj = JObject.Parse(await http.GetStringAsync(reqString).ConfigureAwait(false));
                    var items = obj["items"] as JArray;
                    await channel.SendMessageAsync(items[0]["link"].ToString()).ConfigureAwait(false);
                }
            }
            catch (HttpRequestException exception)
            {
                if (exception.Message.Contains("403 (Forbidden)"))
                {
                    await channel.SendMessageAsync("Daily limit reached!");
                }
                else
                {
                    await channel.SendMessageAsync("Something went wrong.");
                }
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Lmgtfy(IMessage imsg, [Remainder] string ffs = null)
        {
            var channel = (ITextChannel)imsg.Channel;


            if (string.IsNullOrWhiteSpace(ffs))
                return;

            await channel.SendMessageAsync(await _google.ShortenUrl($"<http://lmgtfy.com/?q={ Uri.EscapeUriString(ffs) }>"))
                           .ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Google(IMessage imsg, [Remainder] string terms = null)
        {
            var channel = (ITextChannel)imsg.Channel;


            terms = terms?.Trim();
            if (string.IsNullOrWhiteSpace(terms))
                return;
            await channel.SendMessageAsync($"https://google.com/search?q={ WebUtility.UrlEncode(terms).Replace(' ', '+') }")
                           .ConfigureAwait(false);
        }
        ////todo drawing
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Hearthstone(IMessage imsg, [Remainder] string name = null)
        //{
        //    var channel = (ITextChannel)imsg.Channel;
        //    var arg = name;
        //    if (string.IsNullOrWhiteSpace(arg))
        //    {
        //        await channel.SendMessageAsync("💢 Please enter a card name to search for.").ConfigureAwait(false);
        //        return;
        //    }
        //    await imsg.Channel.TriggerTypingAsync().ConfigureAwait(false);
        //    string response = "";
        //    using (var http = new HttpClient())
        //    {
        //        http.DefaultRequestHeaders.Clear();
        //        http.DefaultRequestHeaders.Add("X-Mashape-Key", NadekoBot.Credentials.MashapeKey);
        //        response = await http.GetStringAsync($"https://omgvamp-hearthstone-v1.p.mashape.com/cards/search/{Uri.EscapeUriString(arg)}", headers)
        //                                .ConfigureAwait(false);
        //        try
        //        {
        //            var items = JArray.Parse(response).Shuffle().ToList();
        //            var images = new List<Image>();
        //            if (items == null)
        //                throw new KeyNotFoundException("Cannot find a card by that name");
        //            var cnt = 0;
        //            foreach (var item in items.TakeWhile(item => cnt++ < 4).Where(item => item.HasValues && item["img"] != null))
        //            {
        //                images.Add(
        //                    Image.FromStream(await http.GetStreamAsync(item["img"].ToString()).ConfigureAwait(false)));
        //            }
        //            if (items.Count > 4)
        //            {
        //                await channel.SendMessageAsync("⚠ Found over 4 images. Showing random 4.").ConfigureAwait(false);
        //            }
        //            await channel.SendMessageAsync(arg + ".png", (await images.MergeAsync()).ToStream(System.Drawing.Imaging.ImageFormat.Png))
        //                           .ConfigureAwait(false);
        //        }
        //        catch (Exception ex)
        //        {
        //            await channel.SendMessageAsync($"💢 Error {ex.Message}").ConfigureAwait(false);
        //        }
        //    }
        //}

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Ud(IMessage imsg, [Remainder] string query = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            var arg = query;
            if (string.IsNullOrWhiteSpace(arg))
            {
                await channel.SendMessageAsync("💢 Please enter a search term.").ConfigureAwait(false);
                return;
            }
            await imsg.Channel.TriggerTypingAsync().ConfigureAwait(false);
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("X-Mashape-Key", NadekoBot.Credentials.MashapeKey);
                var res = await http.GetStringAsync($"https://mashape-community-urban-dictionary.p.mashape.com/define?term={Uri.EscapeUriString(arg)}").ConfigureAwait(false);
                try
                {
                    var items = JObject.Parse(res);
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"`Term:` {items["list"][0]["word"].ToString()}");
                    sb.AppendLine($"`Definition:` {items["list"][0]["definition"].ToString()}");
                    sb.Append($"`Link:` <{await _google.ShortenUrl(items["list"][0]["permalink"].ToString()).ConfigureAwait(false)}>");
                    await channel.SendMessageAsync(sb.ToString());
                }
                catch
                {
                    await channel.SendMessageAsync("💢 Failed finding a definition for that term.").ConfigureAwait(false);
                }
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Hashtag(IMessage imsg, [Remainder] string query = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            var arg = query;
            if (string.IsNullOrWhiteSpace(arg))
            {
                await channel.SendMessageAsync("💢 Please enter a search term.").ConfigureAwait(false);
                return;
            }
            await imsg.Channel.TriggerTypingAsync().ConfigureAwait(false);
            string res = "";
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("X-Mashape-Key", NadekoBot.Credentials.MashapeKey);
                res = await http.GetStringAsync($"https://tagdef.p.mashape.com/one.{Uri.EscapeUriString(arg)}.json").ConfigureAwait(false);
            }

            try
            {
                var items = JObject.Parse(res);
                var str = $@"`Hashtag:` {items["defs"]["def"]["hashtag"].ToString()}
`Definition:` {items["defs"]["def"]["text"].ToString()}
`Link:` <{await _google.ShortenUrl(items["defs"]["def"]["uri"].ToString()).ConfigureAwait(false)}>";
                await channel.SendMessageAsync(str);
            }
            catch
            {
                await channel.SendMessageAsync("💢 Failed finding a definition for that tag.").ConfigureAwait(false);
            }
        }
        //todo DB
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Quote(IMessage imsg)
        //{
        //    var channel = (ITextChannel)imsg.Channel;

        //    var quote = NadekoBot.Config.Quotes[rng.Next(0, NadekoBot.Config.Quotes.Count)].ToString();
        //    await channel.SendMessageAsync(quote).ConfigureAwait(false);
        //}

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Catfact(IMessage imsg)
        {
            var channel = (ITextChannel)imsg.Channel;
            using (var http = new HttpClient())
            {
                var response = await http.GetStringAsync("http://catfacts-api.appspot.com/api/facts").ConfigureAwait(false);
                if (response == null)
                    return;
                await channel.SendMessageAsync($"🐈 `{JObject.Parse(response)["facts"][0].ToString()}`").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Revav(IMessage imsg, [Remainder] string arg = null)
        {
            var channel = (ITextChannel)imsg.Channel;
            var usrStr = arg?.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(usrStr))
                return;

            var usr = (await channel.Guild.GetUsersAsync()).Where(u => u.Username.ToUpperInvariant() == usrStr).FirstOrDefault();

            if (usr == null || string.IsNullOrWhiteSpace(usr.AvatarUrl))
                return;
            await channel.SendMessageAsync($"https://images.google.com/searchbyimage?image_url={usr.AvatarUrl}").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Revimg(IMessage imsg, [Remainder] string imageLink = null)
        {
            var channel = (ITextChannel)imsg.Channel;
            imageLink = imageLink?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(imageLink))
                return;
            await channel.SendMessageAsync($"https://images.google.com/searchbyimage?image_url={imageLink}").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Safebooru(IMessage imsg, [Remainder] string tag = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            tag = tag?.Trim() ?? "";
            var link = await GetSafebooruImageLink(tag).ConfigureAwait(false);
            if (link == null)
                await channel.SendMessageAsync("`No results.`");
            else
                await channel.SendMessageAsync(link).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Wiki(IMessage imsg, [Remainder] string query = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            query = query?.Trim();
            if (string.IsNullOrWhiteSpace(query))
                return;
            using (var http = new HttpClient())
            {
                var result = await http.GetStringAsync("https://en.wikipedia.org//w/api.php?action=query&format=json&prop=info&redirects=1&formatversion=2&inprop=url&titles=" + Uri.EscapeDataString(query));
                var data = JsonConvert.DeserializeObject<WikipediaApiModel>(result);
                if (data.Query.Pages[0].Missing)
                    await channel.SendMessageAsync("`That page could not be found.`");
                else
                    await channel.SendMessageAsync(data.Query.Pages[0].FullUrl);
            }
        }

        ////todo drawing
        //[LocalizedCommand, LocalizedDescription, LocalizedSummary]
        //[RequireContext(ContextType.Guild)]
        //public async Task Clr(IMessage imsg, [Remainder] string color = null)
        //{
        //    var channel = (ITextChannel)imsg.Channel;

        //    color = color?.Trim().Replace("#", "");
        //    if (string.IsNullOrWhiteSpace((string)color))
        //        return;
        //    var img = new Bitmap(50, 50);

        //    var red = Convert.ToInt32(color.Substring(0, 2), 16);
        //    var green = Convert.ToInt32(color.Substring(2, 2), 16);
        //    var blue = Convert.ToInt32(color.Substring(4, 2), 16);
        //    var brush = new SolidBrush(System.Drawing.Color.FromArgb(red, green, blue));

        //    using (Graphics g = Graphics.FromImage(img))
        //    {
        //        g.FillRectangle(brush, 0, 0, 50, 50);
        //        g.Flush();
        //    }

        //    await channel.SendFileAsync("arg1.png", img.ToStream());
        //}

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Videocall(IMessage imsg, [Remainder] string arg = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            try
            {
                var allUsrs = imsg.MentionedUsers.Append(imsg.Author);
                var allUsrsArray = allUsrs.ToArray();
                var str = allUsrsArray.Aggregate("http://appear.in/", (current, usr) => current + Uri.EscapeUriString(usr.Username[0].ToString()));
                str += new Random().Next();
                foreach (var usr in allUsrsArray)
                {
                    await (await (usr as IGuildUser).CreateDMChannelAsync()).SendMessageAsync(str).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Avatar(IMessage imsg, [Remainder] string mention = null)
        {
            var channel = (ITextChannel)imsg.Channel;

            var usr = imsg.MentionedUsers.FirstOrDefault();
            if (usr == null)
            {
                await channel.SendMessageAsync("Invalid user specified.").ConfigureAwait(false);
                return;
            }
            await channel.SendMessageAsync(await _google.ShortenUrl(usr.AvatarUrl).ConfigureAwait(false)).ConfigureAwait(false);
        }

        public static async Task<string> GetSafebooruImageLink(string tag)
        {
            var rng = new Random();
            var url =
            $"http://safebooru.org/index.php?page=dapi&s=post&q=index&limit=100&tags={tag.Replace(" ", "_")}";
            using (var http = new HttpClient())
            {
                var webpage = await http.GetStringAsync(url).ConfigureAwait(false);
                var matches = Regex.Matches(webpage, "file_url=\"(?<url>.*?)\"");
                if (matches.Count == 0)
                    return null;
                var match = matches[rng.Next(0, matches.Count)];
                return matches[rng.Next(0, matches.Count)].Groups["url"].Value;
            }
        }

        public static async Task<bool> ValidateQuery(ITextChannel ch, string query)
        {
            if (!string.IsNullOrEmpty(query.Trim())) return true;
            await ch.SendMessageAsync("Please specify search parameters.").ConfigureAwait(false);
            return false;
        }
    }
}