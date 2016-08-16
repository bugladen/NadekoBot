using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Services;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NadekoBot.Modules.NSFW
{
    [Module("~", AppendSpace = false)]
    public class NSFWModule : DiscordModule
    {
        public NSFWModule(ILocalization loc, CommandService cmds, IBotConfiguration config, IDiscordClient client) : base(loc, cmds, config, client)
        {
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Hentai(IMessage imsg, [Remainder] string tag)
        {
            var channel = imsg.Channel as IGuildChannel;

            tag = tag?.Trim() ?? "";

            var links = await Task.WhenAll(GetGelbooruImageLink("rating%3Aexplicit+" + tag), GetDanbooruImageLink("rating%3Aexplicit+" + tag)).ConfigureAwait(false);

            if (links.All(l => l == null))
            {
                await imsg.Channel.SendMessageAsync("`No results.`");
                return;
            }

            await imsg.Channel.SendMessageAsync(String.Join("\n\n", links)).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Danbooru(IMessage imsg, [Remainder] string tag)
        {
            var channel = imsg.Channel as IGuildChannel;

            tag = tag?.Trim() ?? "";
            var link = await GetDanbooruImageLink(tag).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(link))
                await imsg.Channel.SendMessageAsync("Search yielded no results ;(");
            else
                await imsg.Channel.SendMessageAsync(link).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Gelbooru(IMessage imsg, [Remainder] string tag)
        {
            var channel = imsg.Channel as IGuildChannel;

            tag = tag?.Trim() ?? "";
            var link = await GetRule34ImageLink(tag).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(link))
                await imsg.Channel.SendMessageAsync("Search yielded no results ;(");
            else
                await imsg.Channel.SendMessageAsync(link).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Rule34(IMessage imsg, [Remainder] string tag)
        {
            var channel = imsg.Channel as IGuildChannel;

            tag = tag?.Trim() ?? "";
            var link = await GetGelbooruImageLink(tag).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(link))
                await imsg.Channel.SendMessageAsync("Search yielded no results ;(");
            else
                await imsg.Channel.SendMessageAsync(link).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task E621(IMessage imsg, [Remainder] string tag)
        {
            var channel = imsg.Channel as IGuildChannel;

            tag = tag?.Trim() ?? "";
            var link = await GetE621ImageLink(tag).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(link))
                await imsg.Channel.SendMessageAsync("Search yielded no results ;(");
            else
                await imsg.Channel.SendMessageAsync(link).ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Cp(IMessage imsg)
        {
            var channel = imsg.Channel as IGuildChannel;

            await imsg.Channel.SendMessageAsync("http://i.imgur.com/MZkY1md.jpg").ConfigureAwait(false);
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Boobs(IMessage imsg)
        {
            var channel = imsg.Channel as IGuildChannel;
            try
            {
                JToken obj;
                using (var http = new HttpClient())
                {
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.oboobs.ru/boobs/{ new Random().Next(0, 9880) }").ConfigureAwait(false))[0];
                }
                await imsg.Channel.SendMessageAsync($"http://media.oboobs.ru/{ obj["preview"].ToString() }").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await imsg.Channel.SendMessageAsync($"💢 {ex.Message}").ConfigureAwait(false);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Butts(IMessage imsg)
        {
            var channel = imsg.Channel as IGuildChannel;

            try
            {
                JToken obj;
                using (var http = new HttpClient())
                {
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.obutts.ru/butts/{ new Random().Next(0, 3873) }").ConfigureAwait(false))[0];
                }
                await imsg.Channel.SendMessageAsync($"http://media.obutts.ru/{ obj["preview"].ToString() }").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await imsg.Channel.SendMessageAsync($"💢 {ex.Message}").ConfigureAwait(false);
            }
        }

        public static async Task<string> GetDanbooruImageLink(string tag)
        {
            var rng = new Random();

            if (tag == "loli") //loli doesn't work for some reason atm
                tag = "flat_chest";

            var link = $"http://danbooru.donmai.us/posts?" +
                        $"page={rng.Next(0, 15)}";
            if (!string.IsNullOrWhiteSpace(tag))
                link += $"&tags={tag.Replace(" ", "_")}";
            using (var http = new HttpClient())
            {
                var webpage = await http.GetStringAsync(link).ConfigureAwait(false);
                var matches = Regex.Matches(webpage, "data-large-file-url=\"(?<id>.*?)\"");

                if (matches.Count == 0)
                    return null;
                return $"http://danbooru.donmai.us" +
                       $"{matches[rng.Next(0, matches.Count)].Groups["id"].Value}";
            }
        }

        public static async Task<string> GetGelbooruImageLink(string tag)
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.202 Safari/535.1");
                http.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

                var webpage = await http.GetStringAsync("http://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=100&tags="+ tag.Replace(" ", "_")).ConfigureAwait(false);
                var matches = Regex.Matches(webpage, "file_url=\"(?<url>.*?)\"");
                if (matches.Count == 0)
                    return null;

                var rng = new Random();
                var match = matches[rng.Next(0, matches.Count)];
                return matches[rng.Next(0, matches.Count)].Groups["url"].Value;
            }
        }

        public static async Task<string> GetRule34ImageLink(string tag)
        {
            var rng = new Random();
            var url =
            $"http://rule34.xxx/index.php?page=dapi&s=post&q=index&limit=100&tags={tag.Replace(" ", "_")}";
            using (var http = new HttpClient())
            {
                var webpage = await http.GetStringAsync(url).ConfigureAwait(false);
                var matches = Regex.Matches(webpage, "file_url=\"(?<url>.*?)\"");
                if (matches.Count == 0)
                    return null;
                var match = matches[rng.Next(0, matches.Count)];
                return "http:" + matches[rng.Next(0, matches.Count)].Groups["url"].Value;
            }
        }


        internal static async Task<string> GetE621ImageLink(string tags)
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Clear();
                    http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.202 Safari/535.1");
                    http.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                    var data = await http.GetStringAsync("http://e621.net/post/index.xml?tags=" + Uri.EscapeUriString(tags) + "%20order:random&limit=1");
                    var doc = XDocument.Load(data);
                    return doc.Descendants("file_url").FirstOrDefault().Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in e621 search: \n" + ex);
                return "Error, do you have too many tags?";
            }
        }
    }
}
