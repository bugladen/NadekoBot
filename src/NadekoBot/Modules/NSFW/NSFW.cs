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
using NadekoBot.Extensions;

namespace NadekoBot.Modules.NSFW
{
    [NadekoModule("NSFW", "~")]
    public class NSFW : DiscordModule
    {
        public NSFW() : base()
        {
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Hentai(IUserMessage umsg, [Remainder] string tag = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            tag = tag?.Trim() ?? "";

            tag = "rating%3Aexplicit+" + tag;

            var rng = new NadekoRandom();
            Task<string> provider = Task.FromResult("");
            switch (rng.Next(0,4))
            {
                case 0:
                    provider = GetDanbooruImageLink(tag);
                    break;
                case 1:
                    provider = GetGelbooruImageLink(tag);
                    break;
                case 2:
                    provider = GetKonachanImageLink(tag);
                    break;
                case 3:
                    provider = GetYandereImageLink(tag);
                    break;
                default:
                    break;
            }
            var link = await provider.ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(link))
                await channel.SendErrorAsync("No results found.").ConfigureAwait(false);
            else
                await channel.SendMessageAsync(link).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task HentaiBomb(IUserMessage umsg, [Remainder] string tag = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            tag = tag?.Trim() ?? "";
            tag = "rating%3Aexplicit+" + tag;

            var links = await Task.WhenAll(GetGelbooruImageLink(tag), 
                                           GetDanbooruImageLink(tag),
                                           GetKonachanImageLink(tag),
                                           GetYandereImageLink(tag)).ConfigureAwait(false);

            if (links.All(l => l == null))
            {
                await channel.SendErrorAsync("No results found.").ConfigureAwait(false);
                return;
            }

            await channel.SendMessageAsync(String.Join("\n\n", links)).ConfigureAwait(false);
        }
        
        public static async Task<string> GetYandereImageLink(string tag)
        {
            var rng = new NadekoRandom();
            var url =
            $"https://yande.re/post.xml?" +
            $"limit=25" +
            $"&page={rng.Next(0, 15)}" +
            $"&tags={tag.Replace(" ", "_")}";
            using (var http = new HttpClient())
            {
                var webpage = await http.GetStringAsync(url).ConfigureAwait(false);
                var matches = Regex.Matches(webpage, "file_url=\"(?<url>.*?)\"");
                //var rating = Regex.Matches(webpage, "rating=\"(?<rate>.*?)\"");
                if (matches.Count == 0)
                    return null;
                return matches[rng.Next(0, matches.Count)].Groups["url"].Value;
            }
        }
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Yandere(IUserMessage umsg, [Remainder] string tag = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            tag = tag?.Trim() ?? "";
            var link = await GetYandereImageLink(tag).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(link))
                await channel.SendErrorAsync("No results found.").ConfigureAwait(false);
            else
                await channel.SendMessageAsync(link).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Danbooru(IUserMessage umsg, [Remainder] string tag = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            tag = tag?.Trim() ?? "";
            var link = await GetDanbooruImageLink(tag).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(link))
                await channel.SendErrorAsync("No results found.").ConfigureAwait(false);
            else
                await channel.SendMessageAsync(link).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Konachan(IUserMessage umsg, [Remainder] string tag = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            tag = tag?.Trim() ?? "";
            var link = await GetKonachanImageLink(tag).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(link))
                await channel.SendErrorAsync("No results found.").ConfigureAwait(false);
            else
                await channel.SendMessageAsync(link).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Gelbooru(IUserMessage umsg, [Remainder] string tag = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            tag = tag?.Trim() ?? "";
            var link = await GetGelbooruImageLink(tag).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(link))
                await channel.SendErrorAsync("No results found.").ConfigureAwait(false);
            else
                await channel.SendMessageAsync(link).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Rule34(IUserMessage umsg, [Remainder] string tag = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            tag = tag?.Trim() ?? "";
            var link = await GetRule34ImageLink(tag).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(link))
                await channel.SendErrorAsync("No results found.").ConfigureAwait(false);
            else
                await channel.SendMessageAsync(link).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task E621(IUserMessage umsg, [Remainder] string tag = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            tag = tag?.Trim() ?? "";
            var link = await GetE621ImageLink(tag).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(link))
                await channel.SendErrorAsync("No results found.").ConfigureAwait(false);
            else
                await channel.SendMessageAsync(link).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Cp(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            await channel.SendMessageAsync("http://i.imgur.com/MZkY1md.jpg").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Boobs(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;
            try
            {
                JToken obj;
                using (var http = new HttpClient())
                {
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.oboobs.ru/boobs/{ new NadekoRandom().Next(0, 10229) }").ConfigureAwait(false))[0];
                }
                await channel.SendMessageAsync($"http://media.oboobs.ru/{ obj["preview"].ToString() }").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Butts(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;

            try
            {
                JToken obj;
                using (var http = new HttpClient())
                {
                    obj = JArray.Parse(await http.GetStringAsync($"http://api.obutts.ru/butts/{ new NadekoRandom().Next(0, 4222) }").ConfigureAwait(false))[0];
                }
                await channel.SendMessageAsync($"http://media.obutts.ru/{ obj["preview"].ToString() }").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await channel.SendErrorAsync(ex.Message).ConfigureAwait(false);
            }
        }

        public static async Task<string> GetKonachanImageLink(string tag)
        {
            var rng = new NadekoRandom();

            var link = $"http://konachan.com/post?" +
                        $"page={rng.Next(0, 5)}";
            if (!string.IsNullOrWhiteSpace(tag))
                link += $"&tags={tag.Replace(" ", "_")}";
            using (var http = new HttpClient())
            {
                var webpage = await http.GetStringAsync(link).ConfigureAwait(false);
                var matches = Regex.Matches(webpage, "<a class=\"directlink largeimg\" href=\"(?<ll>.*?)\">");

                if (matches.Count == 0)
                    return null;
                return matches[rng.Next(0, matches.Count)].Groups["ll"].Value;
            }
        }

        public static async Task<string> GetDanbooruImageLink(string tag)
        {
            var rng = new NadekoRandom();

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
                http.AddFakeHeaders();

                var webpage = await http.GetStringAsync("http://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=100&tags="+ tag.Replace(" ", "_")).ConfigureAwait(false);
                var matches = Regex.Matches(webpage, "file_url=\"(?<url>.*?)\"");
                if (matches.Count == 0)
                    return null;

                var rng = new NadekoRandom();
                var match = matches[rng.Next(0, matches.Count)];
                return matches[rng.Next(0, matches.Count)].Groups["url"].Value;
            }
        }

        public static async Task<string> GetRule34ImageLink(string tag)
        {
            var rng = new NadekoRandom();
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


        public static async Task<string> GetE621ImageLink(string tags)
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.AddFakeHeaders();
                    var data = await http.GetStreamAsync("http://e621.net/post/index.xml?tags=" + Uri.EscapeUriString(tags) + "%20order:random&limit=1");
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
