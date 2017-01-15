using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using System.Net.Http;
using NadekoBot.Services;
using System.Threading.Tasks;
using NadekoBot.Attributes;
using System.Text.RegularExpressions;
using System.Net;
using NadekoBot.Modules.Searches.Models;
using System.Collections.Generic;
using ImageSharp;
using NadekoBot.Extensions;
using System.IO;
using NadekoBot.Modules.Searches.Commands.OMDB;
using NadekoBot.Modules.Searches.Commands.Models;
using AngleSharp.Parser.Html;
using AngleSharp;
using AngleSharp.Dom.Html;
using AngleSharp.Dom;
using System.Xml;
using System.Xml.Linq;

namespace NadekoBot.Modules.Searches
{
    [NadekoModule("Searches", "~")]
    public partial class Searches : DiscordModule
    {
        [NadekoCommand, Usage, Description, Aliases]
        public async Task Weather([Remainder] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;

            string response;
            using (var http = new HttpClient())
                response = await http.GetStringAsync($"http://api.openweathermap.org/data/2.5/weather?q={query}&appid=42cd627dd60debf25a5739e50a217d74&units=metric").ConfigureAwait(false);

            var data = JsonConvert.DeserializeObject<WeatherData>(response);

            var embed = new EmbedBuilder()
                .AddField(fb => fb.WithName("🌍 **Location**").WithValue(data.name + ", " + data.sys.country).WithIsInline(true))
                .AddField(fb => fb.WithName("📏 **Lat,Long**").WithValue($"{data.coord.lat}, {data.coord.lon}").WithIsInline(true))
                .AddField(fb => fb.WithName("☁ **Condition**").WithValue(String.Join(", ", data.weather.Select(w => w.main))).WithIsInline(true))
                .AddField(fb => fb.WithName("😓 **Humidity**").WithValue($"{data.main.humidity}%").WithIsInline(true))
                .AddField(fb => fb.WithName("💨 **Wind Speed**").WithValue(data.wind.speed + " km/h").WithIsInline(true))
                .AddField(fb => fb.WithName("🌡 **Temperature**").WithValue(data.main.temp + "°C").WithIsInline(true))
                .AddField(fb => fb.WithName("🔆 **Min - Max**").WithValue($"{data.main.temp_min}°C - {data.main.temp_max}°C").WithIsInline(true))
                .AddField(fb => fb.WithName("🌄 **Sunrise (utc)**").WithValue($"{data.sys.sunrise.ToUnixTimestamp():HH:mm}").WithIsInline(true))
                .AddField(fb => fb.WithName("🌇 **Sunset (utc)**").WithValue($"{data.sys.sunset.ToUnixTimestamp():HH:mm}").WithIsInline(true))
                .WithOkColor()
                .WithFooter(efb => efb.WithText("Powered by http://openweathermap.org"));
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Youtube([Remainder] string query = null)
        {
            if (!(await ValidateQuery(Context.Channel, query).ConfigureAwait(false))) return;
            var result = (await NadekoBot.Google.GetVideosByKeywordsAsync(query, 1)).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(result))
            {
                await Context.Channel.SendErrorAsync("No results found for that query.").ConfigureAwait(false);
                return;
            }

            await Context.Channel.SendMessageAsync(result).ConfigureAwait(false);

            //await Context.Channel.EmbedAsync(new Discord.API.Embed() { Video = new Discord.API.EmbedVideo() { Url = result.Replace("watch?v=", "embed/") }, Color = NadekoBot.OkColor }).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Imdb([Remainder] string query = null)
        {
            if (!(await ValidateQuery(Context.Channel, query).ConfigureAwait(false))) return;
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);

            var movie = await OmdbProvider.FindMovie(query);
            if (movie == null)
            {
                await Context.Channel.SendErrorAsync("Failed to find that movie.").ConfigureAwait(false);
                return;
            }
            await Context.Channel.EmbedAsync(movie.GetEmbed()).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task RandomCat()
        {
            using (var http = new HttpClient())
            {
                var res = JObject.Parse(await http.GetStringAsync("http://www.random.cat/meow").ConfigureAwait(false));
                await Context.Channel.SendMessageAsync(res["file"].ToString()).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task RandomDog()
        {
            using (var http = new HttpClient())
            {
                await Context.Channel.SendMessageAsync("http://random.dog/" + await http.GetStringAsync("http://random.dog/woof")
                             .ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Image([Remainder] string terms = null)
        {
            terms = terms?.Trim();
            if (string.IsNullOrWhiteSpace(terms))
                return;

            terms = WebUtility.UrlEncode(terms).Replace(' ', '+');

            var fullQueryLink = $"http://imgur.com/search?q={ terms }";
            var config = Configuration.Default.WithDefaultLoader();
            var document = await BrowsingContext.New(config).OpenAsync(fullQueryLink);

            var elems = document.QuerySelectorAll("a.image-list-link");

            if (!elems.Any())
                return;

            var img = (elems.FirstOrDefault()?.Children?.FirstOrDefault() as IHtmlImageElement);

            if (img?.Source == null)
                return;

            var source = img.Source.Replace("b.", ".");

            var embed = new EmbedBuilder()
                .WithOkColor()
                .WithAuthor(eab => eab.WithName("Image Search For: " + terms.TrimTo(50))
                    .WithUrl(fullQueryLink)
                    .WithIconUrl("http://s.imgur.com/images/logo-1200-630.jpg?"))
                .WithDescription(source)
                .WithImageUrl(source)
                .WithTitle(Context.User.Mention);
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task RandomImage([Remainder] string terms = null)
        {
            terms = terms?.Trim();
            if (string.IsNullOrWhiteSpace(terms))
                return;

            terms = WebUtility.UrlEncode(terms).Replace(' ', '+');

            var fullQueryLink = $"http://imgur.com/search?q={ terms }";
            var config = Configuration.Default.WithDefaultLoader();
            var document = await BrowsingContext.New(config).OpenAsync(fullQueryLink);

            var elems = document.QuerySelectorAll("a.image-list-link").ToList();

            if (!elems.Any())
                return;

            var img = (elems.ElementAtOrDefault(new NadekoRandom().Next(0, elems.Count))?.Children?.FirstOrDefault() as IHtmlImageElement);

            if (img?.Source == null)
                return;

            var source = img.Source.Replace("b.", ".");

            var embed = new EmbedBuilder()
                .WithOkColor()
                .WithAuthor(eab => eab.WithName("Image Search For: " + terms.TrimTo(50))
                    .WithUrl(fullQueryLink)
                    .WithIconUrl("http://s.imgur.com/images/logo-1200-630.jpg?"))
                .WithDescription(source)
                .WithImageUrl(source)
                .WithTitle(Context.User.Mention);
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Lmgtfy([Remainder] string ffs = null)
        {
            if (string.IsNullOrWhiteSpace(ffs))
                return;

            await Context.Channel.SendConfirmAsync(await NadekoBot.Google.ShortenUrl($"<http://lmgtfy.com/?q={ Uri.EscapeUriString(ffs) }>"))
                           .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Shorten([Remainder] string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                return;

            var shortened = await NadekoBot.Google.ShortenUrl(arg).ConfigureAwait(false);

            if (shortened == arg)
            {
                await Context.Channel.SendErrorAsync("Failed to shorten that url.").ConfigureAwait(false);
            }

            await Context.Channel.EmbedAsync(new EmbedBuilder().WithColor(NadekoBot.OkColor)
                                                           .AddField(efb => efb.WithName("Original Url")
                                                                               .WithValue($"<{arg}>"))
                                                            .AddField(efb => efb.WithName("Short Url")
                                                                                .WithValue($"<{shortened}>")))
                                                            .ConfigureAwait(false);
        }

        //private readonly Regex googleSearchRegex = new Regex(@"<h3 class=""r""><a href=""(?:\/url?q=)?(?<link>.*?)"".*?>(?<title>.*?)<\/a>.*?class=""st"">(?<text>.*?)<\/span>", RegexOptions.Compiled);
        //private readonly Regex htmlReplace = new Regex(@"(?:<b>(.*?)<\/b>|<em>(.*?)<\/em>)", RegexOptions.Compiled);

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Google([Remainder] string terms = null)
        {
            terms = terms?.Trim();
            if (string.IsNullOrWhiteSpace(terms))
                return;

            terms = WebUtility.UrlEncode(terms).Replace(' ', '+');

            var fullQueryLink = $"https://www.google.com/search?q={ terms }&gws_rd=cr,ssl";
            var config = Configuration.Default.WithDefaultLoader();
            var document = await BrowsingContext.New(config).OpenAsync(fullQueryLink);

            var elems = document.QuerySelectorAll("div.g");

            var resultsElem = document.QuerySelectorAll("#resultStats").FirstOrDefault();
            var totalResults = resultsElem?.TextContent;
            //var time = resultsElem.Children.FirstOrDefault()?.TextContent
            //^ this doesn't work for some reason, <nobr> is completely missing in parsed collection
            if (!elems.Any())
                return;

            var results = elems.Select<IElement, GoogleSearchResult?>(elem =>
            {
                var aTag = (elem.Children.FirstOrDefault().Children.FirstOrDefault() as IHtmlAnchorElement); // <h3> -> <a>
                var href = aTag?.Href;
                var name = aTag?.TextContent;
                if (href == null || name == null)
                    return null;

                var txt = elem.QuerySelectorAll(".st").FirstOrDefault()?.TextContent;

                if (txt == null)
                    return null;

                return new GoogleSearchResult(name, href, txt);
            }).Where(x => x != null).Take(5);

            var embed = new EmbedBuilder()
                .WithOkColor()
                .WithAuthor(eab => eab.WithName("Search For: " + terms.TrimTo(50))
                    .WithUrl(fullQueryLink)
                    .WithIconUrl("http://i.imgur.com/G46fm8J.png"))
                .WithTitle(Context.User.Mention)
                .WithFooter(efb => efb.WithText(totalResults));

            var desc = await Task.WhenAll(results.Select(async res => 
                    $"[{Format.Bold(res?.Title)}]({(await NadekoBot.Google.ShortenUrl(res?.Link))})\n{res?.Text}\n\n"))
                .ConfigureAwait(false);
            await Context.Channel.EmbedAsync(embed.WithDescription(String.Concat(desc))).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task MagicTheGathering([Remainder] string name = null)
        {
            var arg = name;
            if (string.IsNullOrWhiteSpace(arg))
            {
                await Context.Channel.SendErrorAsync("Please enter a card name to search for.").ConfigureAwait(false);
                return;
            }

            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            string response = "";
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                response = await http.GetStringAsync($"https://api.deckbrew.com/mtg/cards?name={Uri.EscapeUriString(arg)}")
                                        .ConfigureAwait(false);
                try
                {
                    var items = JArray.Parse(response).Shuffle().ToList();
                    if (items == null)
                        throw new KeyNotFoundException("Cannot find a card by that name");
                    var item = items[0];
                    var storeUrl = await NadekoBot.Google.ShortenUrl(item["store_url"].ToString());
                    var cost = item["cost"].ToString();
                    var desc = item["text"].ToString();
                    var types = String.Join(",\n", item["types"].ToObject<string[]>());
                    var img = item["editions"][0]["image_url"].ToString();
                    var embed = new EmbedBuilder().WithOkColor()
                                    .WithTitle(item["name"].ToString())
                                    .WithDescription(desc)
                                    .WithImageUrl(img)
                                    .AddField(efb => efb.WithName("Store Url").WithValue(storeUrl).WithIsInline(true))
                                    .AddField(efb => efb.WithName("Cost").WithValue(cost).WithIsInline(true))
                                    .AddField(efb => efb.WithName("Types").WithValue(types).WithIsInline(true));
                    //.AddField(efb => efb.WithName("Store Url").WithValue(await NadekoBot.Google.ShortenUrl(items[0]["store_url"].ToString())).WithIsInline(true));

                    await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorAsync($"Error could not find the card '{arg}'.").ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Hearthstone([Remainder] string name = null)
        {
            var arg = name;
            if (string.IsNullOrWhiteSpace(arg))
            {
                await Context.Channel.SendErrorAsync("Please enter a card name to search for.").ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.MashapeKey))
            {
                await Context.Channel.SendErrorAsync("Bot owner didn't specify MashapeApiKey. You can't use this functionality.").ConfigureAwait(false);
                return;
            }

            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            string response = "";
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("X-Mashape-Key", NadekoBot.Credentials.MashapeKey);
                response = await http.GetStringAsync($"https://omgvamp-hearthstone-v1.p.mashape.com/cards/search/{Uri.EscapeUriString(arg)}")
                                        .ConfigureAwait(false);
                try
                {
                    var items = JArray.Parse(response).Shuffle().ToList();
                    var images = new List<ImageSharp.Image>();
                    if (items == null)
                        throw new KeyNotFoundException("Cannot find a card by that name");
                    foreach (var item in items.Where(item => item.HasValues && item["img"] != null).Take(4))
                    {
                        using (var sr = await http.GetStreamAsync(item["img"].ToString()))
                        {
                            var imgStream = new MemoryStream();
                            await sr.CopyToAsync(imgStream);
                            imgStream.Position = 0;
                            images.Add(new ImageSharp.Image(imgStream));
                        }
                    }
                    string msg = null;
                    if (items.Count > 4)
                    {
                        msg = "⚠ Found over 4 images. Showing random 4.";
                    }
                    var ms = new MemoryStream();
                    images.AsEnumerable().Merge().SaveAsPng(ms);
                    ms.Position = 0;
                    await Context.Channel.SendFileAsync(ms, arg + ".png", msg).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendErrorAsync($"Error occured.").ConfigureAwait(false);
                    _log.Error(ex);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Yodify([Remainder] string query = null)
        {
            if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.MashapeKey))
            {
                await Context.Channel.SendErrorAsync("Bot owner didn't specify MashapeApiKey. You can't use this functionality.").ConfigureAwait(false);
                return;
            }

            var arg = query;
            if (string.IsNullOrWhiteSpace(arg))
            {
                await Context.Channel.SendErrorAsync("Please enter a sentence.").ConfigureAwait(false);
                return;
            }
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("X-Mashape-Key", NadekoBot.Credentials.MashapeKey);
                http.DefaultRequestHeaders.Add("Accept", "text/plain");
                var res = await http.GetStringAsync($"https://yoda.p.mashape.com/yoda?sentence={Uri.EscapeUriString(arg)}").ConfigureAwait(false);
                try
                {
                    var embed = new EmbedBuilder()
                        .WithUrl("http://www.yodaspeak.co.uk/")
                        .WithAuthor(au => au.WithName("Yoda").WithIconUrl("http://www.yodaspeak.co.uk/yoda-small1.gif"))
                        .WithDescription(res)
                        .WithOkColor();
                    await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorAsync("Failed to yodify your sentence.").ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task UrbanDict([Remainder] string query = null)
        {
            if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.MashapeKey))
            {
                await Context.Channel.SendErrorAsync("Bot owner didn't specify MashapeApiKey. You can't use this functionality.").ConfigureAwait(false);
                return;
            }

            var arg = query;
            if (string.IsNullOrWhiteSpace(arg))
            {
                await Context.Channel.SendErrorAsync("Please enter a search term.").ConfigureAwait(false);
                return;
            }
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Accept", "application/json");
                var res = await http.GetStringAsync($"http://api.urbandictionary.com/v0/define?term={Uri.EscapeUriString(arg)}").ConfigureAwait(false);
                try
                {
                    var items = JObject.Parse(res);
                    var item = items["list"][0];
                    var word = item["word"].ToString();
                    var def = item["definition"].ToString();
                    var link = item["permalink"].ToString();
                    var embed = new EmbedBuilder().WithOkColor()
                                     .WithUrl(link)
                                     .WithAuthor(eab => eab.WithIconUrl("http://i.imgur.com/nwERwQE.jpg").WithName(word))
                                     .WithDescription(def);
                    await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorAsync("Failed finding a definition for that term.").ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Define([Remainder] string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return;

            using (var http = new HttpClient())
            {
                var res = await http.GetStringAsync("http://api.pearson.com/v2/dictionaries/entries?headword=" + WebUtility.UrlEncode(word.Trim())).ConfigureAwait(false);

                var data = JsonConvert.DeserializeObject<DefineModel>(res);

                var sense = data.Results.Where(x => x.Senses != null && x.Senses[0].Definition != null).FirstOrDefault()?.Senses[0];

                if (sense?.Definition == null)
                    return;

                string definition = sense.Definition.ToString();
                if (!(sense.Definition is string))
                    definition = ((JArray)JToken.Parse(sense.Definition.ToString())).First.ToString();

                var embed = new EmbedBuilder().WithOkColor()
                    .WithTitle("Define: " + word)
                    .WithDescription(definition)
                    .WithFooter(efb => efb.WithText(sense.Gramatical_info?.type));

                if (sense.Examples != null)
                    embed.AddField(efb => efb.WithName("Example").WithValue(sense.Examples.First().text));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Hashtag([Remainder] string query = null)
        {
            var arg = query;
            if (string.IsNullOrWhiteSpace(arg))
            {
                await Context.Channel.SendErrorAsync("Please enter a search term.").ConfigureAwait(false);
                return;
            }
            if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.MashapeKey))
            {
                await Context.Channel.SendErrorAsync("Bot owner didn't specify MashapeApiKey. You can't use this functionality.").ConfigureAwait(false);
                return;
            }

            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
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
                var item = items["defs"]["def"];
                var hashtag = item["hashtag"].ToString();
                var link = item["uri"].ToString();
                var desc = item["text"].ToString();
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                                                                 .WithAuthor(eab => eab.WithUrl(link)
                                                                                       .WithIconUrl("http://res.cloudinary.com/urbandictionary/image/upload/a_exif,c_fit,h_200,w_200/v1394975045/b8oszuu3tbq7ebyo7vo1.jpg")
                                                                                       .WithName(query))
                                                                 .WithDescription(desc))
                                                                 .ConfigureAwait(false);
            }
            catch
            {
                await Context.Channel.SendErrorAsync("Failed finding a definition for that tag.").ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Catfact()
        {
            using (var http = new HttpClient())
            {
                var response = await http.GetStringAsync("http://catfacts-api.appspot.com/api/facts").ConfigureAwait(false);
                if (response == null)
                    return;

                var fact = JObject.Parse(response)["facts"][0].ToString();
                await Context.Channel.SendConfirmAsync("🐈fact", fact).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Revav([Remainder] IUser usr = null)
        {
            if (usr == null)
                usr = Context.User;
            await Context.Channel.SendConfirmAsync($"https://images.google.com/searchbyimage?image_url={usr.AvatarUrl}").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Revimg([Remainder] string imageLink = null)
        {
            imageLink = imageLink?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(imageLink))
                return;
            await Context.Channel.SendConfirmAsync($"https://images.google.com/searchbyimage?image_url={imageLink}").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public Task Safebooru([Remainder] string tag = null)
            => InternalDapiCommand(Context.Message, tag, DapiSearchType.Safebooru);

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Wiki([Remainder] string query = null)
        {
            query = query?.Trim();
            if (string.IsNullOrWhiteSpace(query))
                return;
            using (var http = new HttpClient())
            {
                var result = await http.GetStringAsync("https://en.wikipedia.org//w/api.php?action=query&format=json&prop=info&redirects=1&formatversion=2&inprop=url&titles=" + Uri.EscapeDataString(query));
                var data = JsonConvert.DeserializeObject<WikipediaApiModel>(result);
                if (data.Query.Pages[0].Missing)
                    await Context.Channel.SendErrorAsync("That page could not be found.").ConfigureAwait(false);
                else
                    await Context.Channel.SendMessageAsync(data.Query.Pages[0].FullUrl).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Color([Remainder] string color = null)
        {
            color = color?.Trim().Replace("#", "");
            if (string.IsNullOrWhiteSpace(color))
                return;
            var img = new ImageSharp.Image(50, 50);

            img.BackgroundColor(new ImageSharp.Color(color));

            await Context.Channel.SendFileAsync(img.ToStream(), $"{color}.png").ConfigureAwait(false); ;
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Videocall([Remainder] params IUser[] users)
        {
            try
            {
                var allUsrs = users.Append(Context.User);
                var allUsrsArray = allUsrs.ToArray();
                var str = allUsrsArray.Aggregate("http://appear.in/", (current, usr) => current + Uri.EscapeUriString(usr.Username[0].ToString()));
                str += new NadekoRandom().Next();
                foreach (var usr in allUsrsArray)
                {
                    await (await (usr as IGuildUser).CreateDMChannelAsync()).SendConfirmAsync(str).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Avatar([Remainder] IUser usr = null)
        {
            if (usr == null)
                usr = Context.User;

            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithTitle($"{usr}'s Avatar")
                .WithImageUrl(usr.AvatarUrl)).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Wikia(string target, [Remainder] string query = null)
        {
            if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(query))
            {
                await Context.Channel.SendErrorAsync("Please enter a target wikia, followed by search query.").ConfigureAwait(false);
                return;
            }
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                try
                {
                    var res = await http.GetStringAsync($"http://www.{Uri.EscapeUriString(target)}.wikia.com/api/v1/Search/List?query={Uri.EscapeUriString(query)}&limit=25&minArticleQuality=10&batch=1&namespaces=0%2C14").ConfigureAwait(false);
                    var items = JObject.Parse(res);
                    var found = items["items"][0];
                    var response = $@"`Title:` {found["title"].ToString()}
`Quality:` {found["quality"]}
`URL:` {await NadekoBot.Google.ShortenUrl(found["url"].ToString()).ConfigureAwait(false)}";
                    await Context.Channel.SendMessageAsync(response).ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorAsync($"Failed finding `{query}`.").ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task MCPing([Remainder] string query = null)
        {
            var arg = query;
            if (string.IsNullOrWhiteSpace(arg))
            {
                await Context.Channel.SendErrorAsync("💢 Please enter a `ip:port`.").ConfigureAwait(false);
                return;
            }
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                string ip = arg.Split(':')[0];
                string port = arg.Split(':')[1];
                var res = await http.GetStringAsync($"https://api.minetools.eu/ping/{Uri.EscapeUriString(ip)}/{Uri.EscapeUriString(port)}").ConfigureAwait(false);
                try
                {
                    var items = JObject.Parse(res);
                    var sb = new StringBuilder();
                    int ping = (int)Math.Ceiling(Double.Parse(items["latency"].ToString()));
                    sb.AppendLine($"`Server:` {arg}");
                    sb.AppendLine($"`Version:` {items["version"]["name"].ToString()} / Protocol {items["version"]["protocol"].ToString()}");
                    sb.AppendLine($"`Description:` {items["description"].ToString()}");
                    sb.AppendLine($"`Online Players:` {items["players"]["online"].ToString()}/{items["players"]["max"].ToString()}");
                    sb.Append($"`Latency:` {ping}");
                    await Context.Channel.SendMessageAsync(sb.ToString());
                }
                catch
                {
                    await Context.Channel.SendErrorAsync($"Failed finding `{arg}`.").ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task MCQ([Remainder] string query = null)
        {
            var arg = query;
            if (string.IsNullOrWhiteSpace(arg))
            {
                await Context.Channel.SendErrorAsync("Please enter `ip:port`.").ConfigureAwait(false);
                return;
            }
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                try
                {
                    string ip = arg.Split(':')[0];
                    string port = arg.Split(':')[1];
                    var res = await http.GetStringAsync($"https://api.minetools.eu/query/{Uri.EscapeUriString(ip)}/{Uri.EscapeUriString(port)}").ConfigureAwait(false);
                    var items = JObject.Parse(res);
                    var sb = new StringBuilder();
                    sb.AppendLine($"`Server:` {arg.ToString()} 〘Status: {items["status"]}〙");
                    sb.AppendLine($"`Player List (First 5):`");
                    foreach (var item in items["Playerlist"].Take(5))
                    {
                        sb.AppendLine($"〔:rosette: {item}〕");
                    }
                    sb.AppendLine($"`Online Players:` {items["Players"]} / {items["MaxPlayers"]}");
                    sb.AppendLine($"`Plugins:` {items["Plugins"]}");
                    sb.Append($"`Version:` {items["Version"]}");
                    await Context.Channel.SendMessageAsync(sb.ToString());
                }
                catch
                {
                    await Context.Channel.SendErrorAsync($"Failed finding server `{arg}`.").ConfigureAwait(false);
                }
            }
        }

        public enum DapiSearchType
        {
            Safebooru,
            Gelbooru,
            Konachan,
            Rule34,
            Yandere
        }

        public static async Task InternalDapiCommand(IUserMessage umsg, string tag, DapiSearchType type)
        {
            var channel = umsg.Channel;

            tag = tag?.Trim() ?? "";

            var url = await InternalDapiSearch(tag, type).ConfigureAwait(false);

            if (url == null)
                await channel.SendErrorAsync(umsg.Author.Mention + " No results.");
            else
                await channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription(umsg.Author.Mention + " " + tag)
                    .WithImageUrl(url)
                    .WithFooter(efb => efb.WithText(type.ToString()))).ConfigureAwait(false);
        }

        public static async Task<string> InternalDapiSearch(string tag, DapiSearchType type)
        {
            tag = tag?.Replace(" ", "_");
            string website = "";
            switch (type)
            {
                case DapiSearchType.Safebooru:
                    website = $"https://safebooru.org/index.php?page=dapi&s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Gelbooru:
                    website = $"http://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Rule34:
                    website = $"https://rule34.xxx/index.php?page=dapi&s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Konachan:
                    website = $"https://konachan.com/post.xml?s=post&q=index&limit=100&tags={tag}";
                    break;
                case DapiSearchType.Yandere:
                    website = $"https://yande.re/post.xml?limit=100&tags={tag}";
                    break;
            }
            try
            {
                var toReturn = await Task.Run(async () =>
                {
                    using (var http = new HttpClient())
                    {
                        http.AddFakeHeaders();
                        var data = await http.GetStreamAsync(website).ConfigureAwait(false);
                        var doc = new XmlDocument();
                        doc.Load(data);

                        var node = doc.LastChild.ChildNodes[new NadekoRandom().Next(0, doc.LastChild.ChildNodes.Count)];

                        var url = node.Attributes["file_url"].Value;
                        if (!url.StartsWith("http"))
                            url = "https:" + url;
                        return url;
                    }
                }).ConfigureAwait(false);
                return toReturn;
            }
            catch
            {
                return null;
            }
        }
        public static async Task<bool> ValidateQuery(IMessageChannel ch, string query)
        {
            if (!string.IsNullOrEmpty(query.Trim())) return true;
            await ch.SendErrorAsync("Please specify search parameters.").ConfigureAwait(false);
            return false;
        }
    }
}