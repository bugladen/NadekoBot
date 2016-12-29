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
using System.Xml;
using System.Threading;
using System.Collections.Concurrent;

namespace NadekoBot.Modules.NSFW
{
    [NadekoModule("NSFW", "~")]
    public class NSFW : DiscordModule
    {
        //ulong/cancel
        private static ConcurrentDictionary<ulong, Timer> AutoHentaiTimers { get; } = new ConcurrentDictionary<ulong, Timer>();

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
            switch (rng.Next(0, 4))
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
                await channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithImageUrl(link)
                    .WithDescription("Tag: " + tag)
                    .Build()).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task AutoHentai(IUserMessage umsg, int interval = 0, string tags = null)
        {
            Timer t;

            if (interval == 0)
            {
                if (AutoHentaiTimers.TryRemove(umsg.Channel.Id, out t))
                {
                    t.Change(Timeout.Infinite, Timeout.Infinite); //proper way to disable the timer
                    await umsg.Channel.SendConfirmAsync("Autohentai stopped.").ConfigureAwait(false);
                }
                return;
            }

            if (interval < 20)
                return;

            var tagsArr = tags?.Split('|');

            t = new Timer(async (state) =>
            {
                try
                {
                    if (tagsArr == null || tagsArr.Length == 0)
                        await Hentai(umsg, null).ConfigureAwait(false);
                    else
                        await Hentai(umsg, tagsArr[new NadekoRandom().Next(0, tagsArr.Length)]);
                }
                catch { }
            }, null, interval * 1000, interval * 1000);

            AutoHentaiTimers.AddOrUpdate(umsg.Channel.Id, t, (key, old) =>
            {
                old.Change(Timeout.Infinite, Timeout.Infinite);
                return t;
            });

            await umsg.Channel.SendConfirmAsync($"Autohentai started. Reposting every {interval}s with one of the following tags:\n{string.Join(", ", tagsArr)}").ConfigureAwait(false);
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


        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Danbooru(IUserMessage umsg, [Remainder] string tag = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            tag = tag?.Trim() ?? "";

            var url = await GetDanbooruImageLink(tag).ConfigureAwait(false);

            if (url == null)
                await channel.SendErrorAsync(umsg.Author.Mention + " No results.");
            else
                await channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription(umsg.Author.Mention + " " + tag)
                    .WithImageUrl(url)
                    .WithFooter(efb => efb.WithText("Danbooru"))
                    .Build()).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Yandere(IUserMessage umsg, [Remainder] string tag = null)
            => Searches.Searches.InternalDapiCommand(umsg, tag, Searches.Searches.DapiSearchType.Yandere);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Konachan(IUserMessage umsg, [Remainder] string tag = null)
            => Searches.Searches.InternalDapiCommand(umsg, tag, Searches.Searches.DapiSearchType.Konachan);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Gelbooru(IUserMessage umsg, [Remainder] string tag = null)
            => Searches.Searches.InternalDapiCommand(umsg, tag, Searches.Searches.DapiSearchType.Gelbooru);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task Rule34(IUserMessage umsg, [Remainder] string tag = null)
            => Searches.Searches.InternalDapiCommand(umsg, tag, Searches.Searches.DapiSearchType.Rule34);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task E621(IUserMessage umsg, [Remainder] string tag = null)
        {
            var channel = (ITextChannel)umsg.Channel;

            tag = tag?.Trim() ?? "";

            var url = await GetE621ImageLink(tag).ConfigureAwait(false);

            if (url == null)
                await channel.SendErrorAsync(umsg.Author.Mention + " No results.");
            else
                await channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription(umsg.Author.Mention + " " + tag)
                    .WithImageUrl(url)
                    .WithFooter(efb => efb.WithText("e621"))
                    .Build()).ConfigureAwait(false);
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

        public static async Task<string> GetDanbooruImageLink(string tag)
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.AddFakeHeaders();
                    var data = await http.GetStreamAsync("https://danbooru.donmai.us/posts.xml?limit=100&tags=" + tag);
                    var doc = new XmlDocument();
                    doc.Load(data);
                    var nodes = doc.GetElementsByTagName("file-url");

                    var node = nodes[new NadekoRandom().Next(0, nodes.Count)];
                    return "https://danbooru.donmai.us" + node.InnerText;
                }
            }
            catch
            {
                return null;
            }
        }


        public static async Task<string> GetE621ImageLink(string tag)
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.AddFakeHeaders();
                    var data = await http.GetStreamAsync("http://e621.net/post/index.xml?tags=" + tag);
                    var doc = new XmlDocument();
                    doc.Load(data);
                    var nodes = doc.GetElementsByTagName("file_url");

                    var node = nodes[new NadekoRandom().Next(0, nodes.Count)];
                    return node.InnerText;
                }
            }
            catch
            {
                return null;
            }
        }

        public static Task<string> GetYandereImageLink(string tag) =>
            Searches.Searches.InternalDapiSearch(tag, Searches.Searches.DapiSearchType.Yandere);

        public static Task<string> GetKonachanImageLink(string tag) =>
            Searches.Searches.InternalDapiSearch(tag, Searches.Searches.DapiSearchType.Konachan);

        public static Task<string> GetGelbooruImageLink(string tag) =>
            Searches.Searches.InternalDapiSearch(tag, Searches.Searches.DapiSearchType.Gelbooru);

        public static Task<string> GetRule34ImageLink(string tag) =>
            Searches.Searches.InternalDapiSearch(tag, Searches.Searches.DapiSearchType.Rule34);
    }
}