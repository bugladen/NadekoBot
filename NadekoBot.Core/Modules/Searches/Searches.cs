using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using NadekoBot.Core.Services;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;
using NadekoBot.Extensions;
using System.IO;
using AngleSharp;
using AngleSharp.Dom.Html;
using AngleSharp.Dom;
using Configuration = AngleSharp.Configuration;
using Discord.Commands;
using ImageSharp;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Searches.Common;
using NadekoBot.Modules.Searches.Services;
using NadekoBot.Common.Replacements;
using Discord.WebSocket;
using NadekoBot.Core.Modules.Searches.Common;
using SixLabors.Primitives;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches : NadekoTopLevelModule<SearchesService>
    {
        private readonly IBotCredentials _creds;
        private readonly IGoogleApiService _google;

        public Searches(IBotCredentials creds, IGoogleApiService google)
        {
            _creds = creds;
            _google = google;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Crypto(string name)
        {
            name = name?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(name))
                return;
            var cryptos = (await _service.CryptoData().ConfigureAwait(false));
            var crypto = cryptos
                ?.FirstOrDefault(x => x.Id.ToLowerInvariant() == name || x.Name.ToLowerInvariant() == name
                    || x.Symbol.ToLowerInvariant() == name);

            (CryptoData Elem, int Distance)? nearest = null;
            if (crypto == null)
            {
                nearest = cryptos.Select(x => (x, Distance: x.Name.ToLowerInvariant().LevenshteinDistance(name)))
                    .OrderBy(x => x.Distance)
                    .Where(x => x.Distance <= 2)
                    .FirstOrDefault();

                crypto = nearest?.Elem;
            }

            if (crypto == null)
            {
                await ReplyErrorLocalized("crypto_not_found").ConfigureAwait(false);
                return;
            }

            if (nearest != null)
            {
                var embed = new EmbedBuilder()
                        .WithTitle(GetText("crypto_not_found"))
                        .WithDescription(GetText("did_you_mean", Format.Bold($"{crypto.Name} ({crypto.Symbol})")));

                if (!await PromptUserConfirmAsync(embed))
                    return;
            }

            await Context.Channel.EmbedAsync(new EmbedBuilder()
                .WithOkColor()
                .WithTitle($"{crypto.Name} ({crypto.Symbol})")
                .WithThumbnailUrl($"https://files.coinmarketcap.com/static/img/coins/32x32/{crypto.Id}.png")
                .AddField(GetText("market_cap"), $"${crypto.Market_Cap_Usd:n0}", true)
                .AddField(GetText("price"), $"${crypto.Price_Usd}", true)
                .AddField(GetText("volume_24h"), $"${crypto._24h_Volume_Usd:n0}", true)
                .AddField(GetText("change_7d_24h"), $"{crypto.Percent_Change_7d}% / {crypto.Percent_Change_24h}%", true));
        }

        //for anonymasen :^)
        [NadekoCommand, Usage, Description, Aliases]
        public async Task Rip([Remainder]IGuildUser usr)
        {
            using (var pic = await _service.GetRipPictureAsync(usr.Nickname ?? usr.Username, usr.RealAvatarUrl()))
            using (var picStream = pic.ToStream())
            {
                await Context.Channel.SendFileAsync(
                    picStream,
                    "rip.png",
                    $"Rip {Format.Bold(usr.ToString())} \n\t- " +
                        Format.Italics(Context.User.ToString()))
                    .ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Say([Remainder]string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var rep = new ReplacementBuilder()
                        .WithDefault(Context.User, Context.Channel, (SocketGuild)Context.Guild, (DiscordSocketClient)Context.Client)
                        .Build();

            if (CREmbed.TryParse(message, out var embedData))
            {
                rep.Replace(embedData);
                try
                {
                    await Context.Channel.EmbedAsync(embedData.ToEmbed(), embedData.PlainText?.SanitizeMentions() ?? "").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            }
            else
            {
                var msg = rep.Replace(message);
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    await Context.Channel.SendConfirmAsync(msg).ConfigureAwait(false);
                }
            }
        }

        //[NadekoCommand, Usage, Description, Aliases]
        //public async Task Distance(string p1, string p2)
        //{
        //    if (string.IsNullOrWhiteSpace(p1) || string.IsNullOrWhiteSpace(p2))
        //        return;

        //    var embed = new EmbedBuilder();
        //    string[] response;
        //    try
        //    {
        //        response = await Task.WhenAll(
        //            _service.Http.GetStringAsync($"http://api.openweathermap.org/data/2.5/weather?q={p1}&appid=42cd627dd60debf25a5739e50a217d74&units=metric"),
        //            _service.Http.GetStringAsync($"http://api.openweathermap.org/data/2.5/weather?q={p2}&appid=42cd627dd60debf25a5739e50a217d74&units=metric"));


        //        var d1 = JsonConvert.DeserializeObject<WeatherData>(response[0]);
        //        var d2 = JsonConvert.DeserializeObject<WeatherData>(response[1]);

        //        double R = 6371000; // metres
        //        var φ1 = 0.0174533 * d1.Coord.Lat;
        //        var φ2 = 0.0174533 * d2.Coord.Lat;
        //        var Δφ = 0.0174533 * (d2.Coord.Lat - d1.Coord.Lat);
        //        var Δλ = 0.0174533 * (d2.Coord.Lon - d1.Coord.Lon);

        //        var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
        //                Math.Cos(φ1) * Math.Cos(φ2) *
        //                Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
        //        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        //        await ReplyConfirmLocalized("distance", p1, p2, R * c).ConfigureAwait(false);
        //    }
        //    catch
        //    {

        //    }
        //}

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Weather([Remainder] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;

            var embed = new EmbedBuilder();
            string response;
            try
            {
                response = await _service.Http.GetStringAsync($"http://api.openweathermap.org/data/2.5/weather?q={query}&appid=42cd627dd60debf25a5739e50a217d74&units=metric").ConfigureAwait(false);

                var data = JsonConvert.DeserializeObject<WeatherData>(response);

                Func<double, double> f = StandardConversions.CelsiusToFahrenheit;

                embed
                    .AddField(fb => fb.WithName("🌍 " + Format.Bold(GetText("location"))).WithValue($"[{data.Name + ", " + data.Sys.Country}](https://openweathermap.org/city/{data.Id})").WithIsInline(true))
                    .AddField(fb => fb.WithName("📏 " + Format.Bold(GetText("latlong"))).WithValue($"{data.Coord.Lat}, {data.Coord.Lon}").WithIsInline(true))
                    .AddField(fb => fb.WithName("☁ " + Format.Bold(GetText("condition"))).WithValue(string.Join(", ", data.Weather.Select(w => w.Main))).WithIsInline(true))
                    .AddField(fb => fb.WithName("😓 " + Format.Bold(GetText("humidity"))).WithValue($"{data.Main.Humidity}%").WithIsInline(true))
                    .AddField(fb => fb.WithName("💨 " + Format.Bold(GetText("wind_speed"))).WithValue(data.Wind.Speed + " m/s").WithIsInline(true))
                    .AddField(fb => fb.WithName("🌡 " + Format.Bold(GetText("temperature"))).WithValue($"{data.Main.Temp:F1}°C / {f(data.Main.Temp):F1}°F").WithIsInline(true))
                    .AddField(fb => fb.WithName("🔆 " + Format.Bold(GetText("min_max"))).WithValue($"{data.Main.TempMin:F1}°C - {data.Main.TempMax:F1}°C\n{f(data.Main.TempMin):F1}°F - {f(data.Main.TempMax):F1}°F").WithIsInline(true))
                    .AddField(fb => fb.WithName("🌄 " + Format.Bold(GetText("sunrise"))).WithValue($"{data.Sys.Sunrise.ToUnixTimestamp():HH:mm} UTC").WithIsInline(true))
                    .AddField(fb => fb.WithName("🌇 " + Format.Bold(GetText("sunset"))).WithValue($"{data.Sys.Sunset.ToUnixTimestamp():HH:mm} UTC").WithIsInline(true))
                    .WithOkColor()
                    .WithFooter(efb => efb.WithText("Powered by openweathermap.org").WithIconUrl($"http://openweathermap.org/img/w/{data.Weather[0].Icon}.png"));
            }
            catch
            {
                embed.WithDescription(GetText("city_not_found"))
                    .WithErrorColor();
            }
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Time([Remainder] string arg)
        {
            if (string.IsNullOrWhiteSpace(arg) || string.IsNullOrWhiteSpace(_creds.GoogleApiKey))
                return;

            var res = await _service.Http.GetStringAsync($"https://maps.googleapis.com/maps/api/geocode/json?address={arg}&key={_creds.GoogleApiKey}").ConfigureAwait(false);
            var obj = JsonConvert.DeserializeObject<GeolocationResult>(res);

            var currentSeconds = DateTime.UtcNow.UnixTimestamp();
            var timeRes = await _service.Http.GetStringAsync($"https://maps.googleapis.com/maps/api/timezone/json?location={obj.results[0].Geometry.Location.Lat},{obj.results[0].Geometry.Location.Lng}&timestamp={currentSeconds}&key={_creds.GoogleApiKey}").ConfigureAwait(false);
            var timeObj = JsonConvert.DeserializeObject<TimeZoneResult>(timeRes);

            var time = DateTime.UtcNow.AddSeconds(timeObj.DstOffset + timeObj.RawOffset);

            await ReplyConfirmLocalized("time", Format.Bold(obj.results[0].FormattedAddress), Format.Code(time.ToString("HH:mm")), timeObj.TimeZoneName).ConfigureAwait(false);

        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Youtube([Remainder] string query = null)
        {
            if (!await ValidateQuery(Context.Channel, query).ConfigureAwait(false)) return;
            var result = (await _google.GetVideoLinksByKeywordAsync(query, 1)).FirstOrDefault();
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

            var movie = await OmdbProvider.FindMovie(query, _google);
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
            var res = JObject.Parse(await _service.Http.GetStringAsync("http://aws.random.cat/meow").ConfigureAwait(false));
            await Context.Channel.EmbedAsync(new EmbedBuilder()
                .WithOkColor()
                .WithImageUrl(Uri.EscapeUriString(res["file"].ToString())))
                    .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task RandomDog()
        {
            await Context.Channel.EmbedAsync(new EmbedBuilder()
                .WithOkColor()
                .WithImageUrl("https://random.dog/" + await _service.Http.GetStringAsync("http://random.dog/woof")));
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
                var res = await _google.GetImageAsync(terms).ConfigureAwait(false);
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
                var res = await _google.GetImageAsync(terms, new NadekoRandom().Next(0, 50)).ConfigureAwait(false);
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

            await Context.Channel.SendConfirmAsync("<" + await _google.ShortenUrl($"http://lmgtfy.com/?q={ Uri.EscapeUriString(ffs) }") + ">")
                           .ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Shorten([Remainder] string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                return;

            var shortened = await _google.ShortenUrl(arg).ConfigureAwait(false);

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

            var fullQueryLink = $"https://www.google.ca/search?q={ terms }&gws_rd=cr,ssl&cr=countryUS";
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
                    $"[{Format.Bold(res?.Title)}]({(await _google.ShortenUrl(res?.Link))})\n{res?.Text?.TrimTo(400 - res.Value.Title.Length - res.Value.Link.Length)}\n\n"))
                .ConfigureAwait(false);
            var descStr = string.Concat(desc);
            _log.Info(descStr.Length);
            await Context.Channel.EmbedAsync(embed.WithDescription(descStr)).ConfigureAwait(false);
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
                    var storeUrl = await _google.ShortenUrl(item["store_url"].ToString());
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
                    //.AddField(efb => efb.WithName("Store Url").WithValue(await _google.ShortenUrl(items[0]["store_url"].ToString())).WithIsInline(true));

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

            if (string.IsNullOrWhiteSpace(_creds.MashapeKey))
            {
                await ReplyErrorLocalized("mashape_api_missing").ConfigureAwait(false);
                return;
            }

            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("X-Mashape-Key", _creds.MashapeKey);
                var response = await http.GetStringAsync($"https://omgvamp-hearthstone-v1.p.mashape.com/cards/search/{Uri.EscapeUriString(arg)}")
                    .ConfigureAwait(false);
                try
                {
                    var items = JArray.Parse(response).Shuffle().ToList();
                    var images = new List<Image<Rgba32>>();
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
                                images.Add(ImageSharp.Image.Load(imgStream));
                            }
                        }).ConfigureAwait(false);
                    }
                    string msg = null;
                    if (items.Count > 4)
                    {
                        msg = GetText("hs_over_x", 4);
                    }
                    var ms = new MemoryStream();
                    await Task.Run(() => images.AsEnumerable().Merge().SaveAsPng(ms));
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
            if (string.IsNullOrWhiteSpace(_creds.MashapeKey))
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
                http.DefaultRequestHeaders.Add("X-Mashape-Key", _creds.MashapeKey);
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
            if (string.IsNullOrWhiteSpace(_creds.MashapeKey))
            {
                await ReplyErrorLocalized("mashape_api_missing").ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(query))
                return;

            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            var res = await _service.Http.GetStringAsync($"http://api.urbandictionary.com/v0/define?term={Uri.EscapeUriString(query)}").ConfigureAwait(false);
            try
            {
                var items = JsonConvert.DeserializeObject<UrbanResponse>(res).List;
                if (items.Any())
                {

                    await Context.SendPaginatedConfirmAsync(0, (p) =>
                    {
                        var item = items[p];
                        return new EmbedBuilder().WithOkColor()
                                     .WithUrl(item.Permalink)
                                     .WithAuthor(eab => eab.WithIconUrl("http://i.imgur.com/nwERwQE.jpg").WithName(item.Word))
                                     .WithDescription(item.Definition);
                    }, items.Length, 1);
                    return;
                }
            }
            catch
            {
            }
            await ReplyErrorLocalized("ud_error").ConfigureAwait(false);

        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Define([Remainder] string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return;

            var res = await _service.Http.GetStringAsync("http://api.pearson.com/v2/dictionaries/entries?headword=" + WebUtility.UrlEncode(word.Trim())).ConfigureAwait(false);

            var data = JsonConvert.DeserializeObject<DefineModel>(res);

            var sense = data.Results.FirstOrDefault(x => x.Senses?[0].Definition != null)?.Senses[0];

            if (sense?.Definition == null)
            {
                await ReplyErrorLocalized("define_unknown").ConfigureAwait(false);
                return;
            }

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

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Hashtag([Remainder] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;

            if (string.IsNullOrWhiteSpace(_creds.MashapeKey))
            {
                await ReplyErrorLocalized("mashape_api_missing").ConfigureAwait(false);
                return;
            }

            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            string res;
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("X-Mashape-Key", _creds.MashapeKey);
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
            var response = await _service.Http.GetStringAsync("https://catfact.ninja/fact").ConfigureAwait(false);
            if (response == null)
                return;

            var fact = JObject.Parse(response)["fact"].ToString();
            await Context.Channel.SendConfirmAsync("🐈" + GetText("catfact"), fact).ConfigureAwait(false);
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
        public async Task Color(params string[] colors)
        {
            if (!colors.Any())
                return;

            var colorObjects = colors.Take(10).Select(x => x.Trim().Replace("#", ""))
                .Select(x =>
                {
                    try
                    {
                        return Rgba32.FromHex(x);
                    }
                    catch
                    {
                        return Rgba32.FromHex("000");
                    }
                }).ToArray();
            using (var img = new Image<Rgba32>(colorObjects.Length * 50, 50))
            {
                for (int i = 0; i < colorObjects.Length; i++)
                {
                    var x = i * 50;
                    img.FillPolygon(colorObjects[i], new PointF[] {
                        new PointF(x, 0),
                        new PointF(x + 50, 0),
                        new PointF(x + 50, 50),
                        new PointF(x, 50)
                    });
                }
                await Context.Channel.SendFileAsync(img.ToStream(), $"colors.png").ConfigureAwait(false);
            }
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
                await (await usr.GetOrCreateDMChannelAsync()).SendConfirmAsync(str).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Avatar([Remainder] IGuildUser usr = null)
        {
            if (usr == null)
                usr = (IGuildUser)Context.User;

            var avatarUrl = usr.RealAvatarUrl();
            var shortenedAvatarUrl = await _google.ShortenUrl(avatarUrl).ConfigureAwait(false);
            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .AddField(efb => efb.WithName("Username").WithValue(usr.ToString()).WithIsInline(false))
                .AddField(efb => efb.WithName("Avatar Url").WithValue(shortenedAvatarUrl).WithIsInline(false))
                .WithThumbnailUrl(avatarUrl)
                .WithImageUrl(avatarUrl), Context.User.Mention).ConfigureAwait(false);
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
`{GetText("url")}:` {await _google.ShortenUrl(found["url"].ToString()).ConfigureAwait(false)}";
                    await Context.Channel.SendMessageAsync(response).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("wikia_error").ConfigureAwait(false);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Bible(string book, string chapterAndVerse)
        {
            var obj = new BibleVerses();
            try
            {
                var res = await _service.Http
                    .GetStringAsync("https://bible-api.com/" + book + " " + chapterAndVerse);

                obj = JsonConvert.DeserializeObject<BibleVerses>(res);
            }
            catch
            {
            }
            if (obj.Error != null || obj.Verses == null || obj.Verses.Length == 0)
                await Context.Channel.SendErrorAsync(obj.Error ?? "No verse found.").ConfigureAwait(false);
            else
            {
                var v = obj.Verses[0];
                await Context.Channel.EmbedAsync(new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle($"{v.BookName} {v.Chapter}:{v.Verse}")
                    .WithDescription(v.Text));
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


        public async Task InternalDapiCommand(IUserMessage umsg, string tag, DapiSearchType type)
        {
            var channel = umsg.Channel;

            tag = tag?.Trim() ?? "";

            var imgObj = await _service.DapiSearch(tag, type, Context.Guild?.Id).ConfigureAwait(false);

            if (imgObj == null)
                await channel.SendErrorAsync(umsg.Author.Mention + " " + GetText("no_results"));
            else
                await channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription($"{umsg.Author.Mention} [{tag ?? "url"}]({imgObj.FileUrl})")
                    .WithImageUrl(imgObj.FileUrl)
                    .WithFooter(efb => efb.WithText(type.ToString()))).ConfigureAwait(false);
        }

        public async Task<bool> ValidateQuery(IMessageChannel ch, string query)
        {
            if (!string.IsNullOrWhiteSpace(query)) return true;
            await ch.SendErrorAsync(GetText("specify_search_params")).ConfigureAwait(false);
            return false;
        }
    }
}
