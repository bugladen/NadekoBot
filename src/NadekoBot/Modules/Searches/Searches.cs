using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using System.Net.Http;
using NadekoBot.Services;
using System.Threading.Tasks;
using System.Net;
using NadekoBot.Modules.Searches.Models;
using System.Collections.Generic;
using NadekoBot.Extensions;
using System.IO;
using NadekoBot.Modules.Searches.Commands.OMDB;
using NadekoBot.Modules.Searches.Commands.Models;
using AngleSharp;
using AngleSharp.Dom.Html;
using AngleSharp.Dom;
using System.Xml;
using Configuration = AngleSharp.Configuration;
using NadekoBot.Attributes;
using Discord.Commands;
using ImageSharp.Processing.Processors;
using ImageSharp;

namespace NadekoBot.Modules.Searches
{
    [NadekoModule("Searches", "~")]
    public partial class Searches : NadekoTopLevelModule
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
                .AddField(fb => fb.WithName("🌍 " + Format.Bold(GetText("location"))).WithValue($"[{data.name + ", " + data.sys.country}](https://openweathermap.org/city/{data.id})").WithIsInline(true))
                .AddField(fb => fb.WithName("📏 " + Format.Bold(GetText("latlong"))).WithValue($"{data.coord.lat}, {data.coord.lon}").WithIsInline(true))
                .AddField(fb => fb.WithName("☁ " + Format.Bold(GetText("condition"))).WithValue(string.Join(", ", data.weather.Select(w => w.main))).WithIsInline(true))
                .AddField(fb => fb.WithName("😓 " + Format.Bold(GetText("humidity"))).WithValue($"{data.main.humidity}%").WithIsInline(true))
                .AddField(fb => fb.WithName("💨 " + Format.Bold(GetText("wind_speed"))).WithValue(data.wind.speed + " m/s").WithIsInline(true))
                .AddField(fb => fb.WithName("🌡 " + Format.Bold(GetText("temperature"))).WithValue(data.main.temp + "°C").WithIsInline(true))
                .AddField(fb => fb.WithName("🔆 " + Format.Bold(GetText("min_max"))).WithValue($"{data.main.temp_min}°C - {data.main.temp_max}°C").WithIsInline(true))
                .AddField(fb => fb.WithName("🌄 " + Format.Bold(GetText("sunrise"))).WithValue($"{data.sys.sunrise.ToUnixTimestamp():HH:mm} UTC").WithIsInline(true))
                .AddField(fb => fb.WithName("🌇 " + Format.Bold(GetText("sunset"))).WithValue($"{data.sys.sunset.ToUnixTimestamp():HH:mm} UTC").WithIsInline(true))
                .WithOkColor()
                .WithFooter(efb => efb.WithText("Powered by openweathermap.org").WithIconUrl($"http://openweathermap.org/img/w/{data.weather[0].icon}.png"));
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Time([Remainder] string arg)
        {
            if (string.IsNullOrWhiteSpace(arg) || string.IsNullOrWhiteSpace(NadekoBot.Credentials.GoogleApiKey))
                return;

            using (var http = new HttpClient())
            {
                var res = await http.GetStringAsync($"https://maps.googleapis.com/maps/api/geocode/json?address={arg}&key={NadekoBot.Credentials.GoogleApiKey}").ConfigureAwait(false);
                var obj = JsonConvert.DeserializeObject<GeolocationResult>(res);

                var currentSeconds = DateTime.UtcNow.UnixTimestamp();
                var timeRes = await http.GetStringAsync($"https://maps.googleapis.com/maps/api/timezone/json?location={obj.results[0].Geometry.Location.Lat},{obj.results[0].Geometry.Location.Lng}&timestamp={currentSeconds}&key={NadekoBot.Credentials.GoogleApiKey}").ConfigureAwait(false);
                var timeObj = JsonConvert.DeserializeObject<TimeZoneResult>(timeRes);

                var time = DateTime.UtcNow.AddSeconds(timeObj.DstOffset + timeObj.RawOffset);

                await ReplyConfirmLocalized("time", Format.Bold(obj.results[0].FormattedAddress), Format.Code(time.ToString("HH:mm")), timeObj.TimeZoneName).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Youtube([Remainder] string query = null)
        {
            if (!await ValidateQuery(Context.Channel, query).ConfigureAwait(false)) return;
            var result = (await NadekoBot.Google.GetVideosByKeywordsAsync(query, 1)).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(result))
            {
                await ReplyErrorLocalized("no_results").ConfigureAwait(false);
                return;
            }

            await Context.Channel.SendMessageAsync(result).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Imdb([Remainder] string query = null)
        {
            if (!(await ValidateQuery(Context.Channel, query).ConfigureAwait(false))) return;
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);

            var movie = await OmdbProvider.FindMovie(query);
            if (movie == null)
            {
                await ReplyErrorLocalized("imdb_fail").ConfigureAwait(false);
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
                await Context.Channel.SendMessageAsync(Uri.EscapeUriString(res["file"].ToString())).ConfigureAwait(false);
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

            try
            {
                var res = await NadekoBot.Google.GetImageAsync(terms).ConfigureAwait(false);
                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithAuthor(eab => eab.WithName(GetText("image_search_for") + " " + terms.TrimTo(50))
                        .WithUrl("https://www.google.rs/search?q=" + terms + "&source=lnms&tbm=isch")
                        .WithIconUrl("http://i.imgur.com/G46fm8J.png"))
                    .WithDescription(res.Link)
                    .WithImageUrl(res.Link)
                    .WithTitle(Context.User.ToString());
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
            catch
            {
                _log.Warn("Falling back to Imgur search.");

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
                    .WithTitle(Context.User.ToString());
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task RandomImage([Remainder] string terms = null)
        {
            terms = terms?.Trim();
            if (string.IsNullOrWhiteSpace(terms))
                return;
            terms = WebUtility.UrlEncode(terms).Replace(' ', '+');
            try
            {
                var res = await NadekoBot.Google.GetImageAsync(terms, new NadekoRandom().Next(0, 50)).ConfigureAwait(false);
                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithAuthor(eab => eab.WithName(GetText("image_search_for") + " " + terms.TrimTo(50))
                        .WithUrl("https://www.google.rs/search?q=" + terms + "&source=lnms&tbm=isch")
                        .WithIconUrl("http://i.imgur.com/G46fm8J.png"))
                    .WithDescription(res.Link)
                    .WithImageUrl(res.Link)
                    .WithTitle(Context.User.ToString());
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
            catch
            {
                _log.Warn("Falling back to Imgur");
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
                    .WithAuthor(eab => eab.WithName(GetText("image_search_for") + " " + terms.TrimTo(50))
                        .WithUrl(fullQueryLink)
                        .WithIconUrl("http://s.imgur.com/images/logo-1200-630.jpg?"))
                    .WithDescription(source)
                    .WithImageUrl(source)
                    .WithTitle(Context.User.ToString());
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Lmgtfy([Remainder] string ffs = null)
        {
            if (string.IsNullOrWhiteSpace(ffs))
                return;

            await Context.Channel.SendConfirmAsync("<" + await NadekoBot.Google.ShortenUrl($"http://lmgtfy.com/?q={ Uri.EscapeUriString(ffs) }") + ">")
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
                await ReplyErrorLocalized("shorten_fail").ConfigureAwait(false);
                return;
            }

            await Context.Channel.EmbedAsync(new EmbedBuilder().WithColor(NadekoBot.OkColor)
                                                           .AddField(efb => efb.WithName(GetText("original_url"))
                                                                               .WithValue($"<{arg}>"))
                                                            .AddField(efb => efb.WithName(GetText("short_url"))
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
                var aTag = (elem.Children.FirstOrDefault()?.Children.FirstOrDefault() as IHtmlAnchorElement); // <h3> -> <a>
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
                .WithAuthor(eab => eab.WithName(GetText("search_for") + " " + terms.TrimTo(50))
                    .WithUrl(fullQueryLink)
                    .WithIconUrl("http://i.imgur.com/G46fm8J.png"))
                .WithTitle(Context.User.ToString())
                .WithFooter(efb => efb.WithText(totalResults));

            var desc = await Task.WhenAll(results.Select(async res =>
                    $"[{Format.Bold(res?.Title)}]({(await NadekoBot.Google.ShortenUrl(res?.Link))})\n{res?.Text}\n\n"))
                .ConfigureAwait(false);
            await Context.Channel.EmbedAsync(embed.WithDescription(string.Concat(desc))).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task MagicTheGathering([Remainder] string name)
        {
            var arg = name;
            if (string.IsNullOrWhiteSpace(arg))
                return;

            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                var response = await http.GetStringAsync($"https://api.deckbrew.com/mtg/cards?name={Uri.EscapeUriString(arg)}")
                    .ConfigureAwait(false);
                try
                {
                    var items = JArray.Parse(response).ToArray();
                    if (items == null || items.Length == 0)
                        throw new KeyNotFoundException("Cannot find a card by that name");
                    var item = items[new NadekoRandom().Next(0, items.Length)];
                    var storeUrl = await NadekoBot.Google.ShortenUrl(item["store_url"].ToString());
                    var cost = item["cost"].ToString();
                    var desc = item["text"].ToString();
                    var types = string.Join(",\n", item["types"].ToObject<string[]>());
                    var img = item["editions"][0]["image_url"].ToString();
                    var embed = new EmbedBuilder().WithOkColor()
                                    .WithTitle(item["name"].ToString())
                                    .WithDescription(desc)
                                    .WithImageUrl(img)
                                    .AddField(efb => efb.WithName(GetText("store_url")).WithValue(storeUrl).WithIsInline(true))
                                    .AddField(efb => efb.WithName(GetText("cost")).WithValue(cost).WithIsInline(true))
                                    .AddField(efb => efb.WithName(GetText("types")).WithValue(types).WithIsInline(true));
                    //.AddField(efb => efb.WithName("Store Url").WithValue(await NadekoBot.Google.ShortenUrl(items[0]["store_url"].ToString())).WithIsInline(true));

                    await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("card_not_found").ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Hearthstone([Remainder] string name)
        {
            var arg = name;
            if (string.IsNullOrWhiteSpace(arg))
                return;

            if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.MashapeKey))
            {
                await ReplyErrorLocalized("mashape_api_missing").ConfigureAwait(false);
                return;
            }

            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("X-Mashape-Key", NadekoBot.Credentials.MashapeKey);
                var response = await http.GetStringAsync($"https://omgvamp-hearthstone-v1.p.mashape.com/cards/search/{Uri.EscapeUriString(arg)}")
                    .ConfigureAwait(false);
                try
                {
                    var items = JArray.Parse(response).Shuffle().ToList();
                    var images = new List<ImageSharp.Image>();
                    if (items == null)
                        throw new KeyNotFoundException("Cannot find a card by that name");
                    foreach (var item in items.Where(item => item.HasValues && item["img"] != null).Take(4))
                    {
                        await Task.Run(async () =>
                        {
                            using (var sr = await http.GetStreamAsync(item["img"].ToString()))
                            {
                                var imgStream = new MemoryStream();
                                await sr.CopyToAsync(imgStream);
                                imgStream.Position = 0;
                                images.Add(new ImageSharp.Image(imgStream));
                            }
                        }).ConfigureAwait(false);
                    }
                    string msg = null;
                    if (items.Count > 4)
                    {
                        msg = GetText("hs_over_x", 4);
                    }
                    var ms = new MemoryStream();
                    await Task.Run(() => images.AsEnumerable().Merge().Save(ms));
                    ms.Position = 0;
                    await Context.Channel.SendFileAsync(ms, arg + ".png", msg).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    await ReplyErrorLocalized("error_occured").ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Yodify([Remainder] string query = null)
        {
            if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.MashapeKey))
            {
                await ReplyErrorLocalized("mashape_api_missing").ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(query))
                return;

            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("X-Mashape-Key", NadekoBot.Credentials.MashapeKey);
                http.DefaultRequestHeaders.Add("Accept", "text/plain");
                var res = await http.GetStringAsync($"https://yoda.p.mashape.com/yoda?sentence={Uri.EscapeUriString(query)}").ConfigureAwait(false);
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
                    await ReplyErrorLocalized("yodify_error").ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task UrbanDict([Remainder] string query = null)
        {
            if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.MashapeKey))
            {
                await ReplyErrorLocalized("mashape_api_missing").ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(query))
                return;

            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Accept", "application/json");
                var res = await http.GetStringAsync($"http://api.urbandictionary.com/v0/define?term={Uri.EscapeUriString(query)}").ConfigureAwait(false);
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
                    await ReplyErrorLocalized("ud_error").ConfigureAwait(false);
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

                var sense = data.Results.FirstOrDefault(x => x.Senses?[0].Definition != null)?.Senses[0];

                if (sense?.Definition == null)
                    return;

                var definition = sense.Definition.ToString();
                if (!(sense.Definition is string))
                    definition = ((JArray)JToken.Parse(sense.Definition.ToString())).First.ToString();

                var embed = new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("define") + " " + word)
                    .WithDescription(definition)
                    .WithFooter(efb => efb.WithText(sense.Gramatical_info?.type));

                if (sense.Examples != null)
                    embed.AddField(efb => efb.WithName(GetText("example")).WithValue(sense.Examples.First().text));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Hashtag([Remainder] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;

            if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.MashapeKey))
            {
                await ReplyErrorLocalized("mashape_api_missing").ConfigureAwait(false);
                return;
            }

            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            string res;
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("X-Mashape-Key", NadekoBot.Credentials.MashapeKey);
                res = await http.GetStringAsync($"https://tagdef.p.mashape.com/one.{Uri.EscapeUriString(query)}.json").ConfigureAwait(false);
            }

            try
            {
                var items = JObject.Parse(res);
                var item = items["defs"]["def"];
                //var hashtag = item["hashtag"].ToString();
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
                await ReplyErrorLocalized("hashtag_error").ConfigureAwait(false);
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
                await Context.Channel.SendConfirmAsync("🐈" + GetText("catfact"), fact).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Revav([Remainder] IGuildUser usr = null)
        {
            if (usr == null)
                usr = (IGuildUser)Context.User;
            await Context.Channel.SendConfirmAsync($"https://images.google.com/searchbyimage?image_url={usr.RealAvatarUrl()}").ConfigureAwait(false);
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
                    await ReplyErrorLocalized("wiki_page_not_found").ConfigureAwait(false);
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

            img.BackgroundColor(ImageSharp.Color.FromHex(color));

            await Context.Channel.SendFileAsync(img.ToStream(), $"{color}.png").ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Videocall(params IGuildUser[] users)
        {
            var allUsrs = users.Append(Context.User);
            var allUsrsArray = allUsrs.ToArray();
            var str = allUsrsArray.Aggregate("http://appear.in/", (current, usr) => current + Uri.EscapeUriString(usr.Username[0].ToString()));
            str += new NadekoRandom().Next();
            foreach (var usr in allUsrsArray)
            {
                await (await usr.CreateDMChannelAsync()).SendConfirmAsync(str).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Avatar([Remainder] IGuildUser usr = null)
        {
            if (usr == null)
                usr = (IGuildUser)Context.User;

            var avatarUrl = usr.RealAvatarUrl();
            var shortenedAvatarUrl = await NadekoBot.Google.ShortenUrl(avatarUrl).ConfigureAwait(false);
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .AddField(efb => efb.WithName("Username").WithValue(usr.ToString()).WithIsInline(false))
                .AddField(efb => efb.WithName("Avatar Url").WithValue(shortenedAvatarUrl).WithIsInline(false))
                //.AddField(efb => efb.WithName("Avatar Id").WithValue(usr.AvatarId).WithIsInline(false))
                .WithThumbnailUrl(avatarUrl), Context.User.Mention).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Wikia(string target, [Remainder] string query)
        {
            if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(query))
            {
                await ReplyErrorLocalized("wikia_input_error").ConfigureAwait(false);
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
                    var response = $@"`{GetText("title")}` {found["title"]}
`{GetText("quality")}` {found["quality"]}
`{GetText("url")}:` {await NadekoBot.Google.ShortenUrl(found["url"].ToString()).ConfigureAwait(false)}";
                    await Context.Channel.SendMessageAsync(response).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("wikia_error").ConfigureAwait(false);
                }
            }
        }

        //[NadekoCommand, Usage, Description, Aliases]
        //public async Task MCPing([Remainder] string query2 = null)
        //{
        //    var query = query2;
        //    if (string.IsNullOrWhiteSpace(query))
        //        return;
        //    await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
        //    using (var http = new HttpClient())
        //    {
        //        http.DefaultRequestHeaders.Clear();
        //        var ip = query.Split(':')[0];
        //        var port = query.Split(':')[1];
        //        var res = await http.GetStringAsync($"https://api.minetools.eu/ping/{Uri.EscapeUriString(ip)}/{Uri.EscapeUriString(port)}").ConfigureAwait(false);
        //        try
        //        {
        //            var items = JObject.Parse(res);
        //            var sb = new StringBuilder();
        //            var ping = (int)Math.Ceiling(double.Parse(items["latency"].ToString()));
        //            sb.AppendLine($"`Server:` {query}");
        //            sb.AppendLine($"`Version:` {items["version"]["name"]} / Protocol {items["version"]["protocol"]}");
        //            sb.AppendLine($"`Description:` {items["description"]}");
        //            sb.AppendLine($"`Online Players:` {items["players"]["online"]}/{items["players"]["max"]}");
        //            sb.Append($"`Latency:` {ping}");
        //            await Context.Channel.SendMessageAsync(sb.ToString());
        //        }
        //        catch
        //        {
        //            await Context.Channel.SendErrorAsync($"Failed finding `{query}`.").ConfigureAwait(false);
        //        }
        //    }
        //}

        //[NadekoCommand, Usage, Description, Aliases]
        //public async Task MCQ([Remainder] string query = null)
        //{
        //    var arg = query;
        //    if (string.IsNullOrWhiteSpace(arg))
        //    {
        //        await Context.Channel.SendErrorAsync("Please enter `ip:port`.").ConfigureAwait(false);
        //        return;
        //    }
        //    await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
        //    using (var http = new HttpClient())
        //    {
        //        http.DefaultRequestHeaders.Clear();
        //        try
        //        {
        //            var ip = arg.Split(':')[0];
        //            var port = arg.Split(':')[1];
        //            var res = await http.GetStringAsync($"https://api.minetools.eu/query/{Uri.EscapeUriString(ip)}/{Uri.EscapeUriString(port)}").ConfigureAwait(false);
        //            var items = JObject.Parse(res);
        //            var sb = new StringBuilder();
        //            sb.AppendLine($"`Server:` {arg} 〘Status: {items["status"]}〙");
        //            sb.AppendLine("`Player List (First 5):`");
        //            foreach (var item in items["Playerlist"].Take(5))
        //            {
        //                sb.AppendLine($"〔:rosette: {item}〕");
        //            }
        //            sb.AppendLine($"`Online Players:` {items["Players"]} / {items["MaxPlayers"]}");
        //            sb.AppendLine($"`Plugins:` {items["Plugins"]}");
        //            sb.Append($"`Version:` {items["Version"]}");
        //            await Context.Channel.SendMessageAsync(sb.ToString());
        //        }
        //        catch
        //        {
        //            await Context.Channel.SendErrorAsync($"Failed finding server `{arg}`.").ConfigureAwait(false);
        //        }
        //    }
        //}

        public enum DapiSearchType
        {
            Safebooru,
            Gelbooru,
            Konachan,
            Rule34,
            Yandere
        }

        public async Task InternalDapiCommand(IUserMessage umsg, string tag, DapiSearchType type)
        {
            var channel = umsg.Channel;

            tag = tag?.Trim() ?? "";

            var url = await InternalDapiSearch(tag, type).ConfigureAwait(false);

            if (url == null)
                await channel.SendErrorAsync(umsg.Author.Mention + " " + GetText("no_results"));
            else
                await channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription(umsg.Author.Mention + " " + tag)
                    .WithImageUrl(url)
                    .WithFooter(efb => efb.WithText(type.ToString()))).ConfigureAwait(false);
        }

        public static async Task<string> InternalDapiSearch(string tag, DapiSearchType type)
        {
            tag = tag?.Replace(" ", "_");
            var website = "";
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
        public async Task<bool> ValidateQuery(IMessageChannel ch, string query)
        {
            if (!string.IsNullOrWhiteSpace(query)) return true;
            await ch.SendErrorAsync(GetText("specify_search_params")).ConfigureAwait(false);
            return false;
        }
    }
}