using Discord;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.Classes.JSONModels;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Classes;
using NadekoBot.Modules.Searches.Commands;
using NadekoBot.Modules.Searches.Commands.IMDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace NadekoBot.Modules.Searches
{
    internal class SearchesModule : DiscordModule
    {
        private readonly Random rng;
        public SearchesModule()
        {
            commands.Add(new LoLCommands(this));
            commands.Add(new StreamNotifications(this));
            commands.Add(new ConverterCommand(this));
            commands.Add(new RedditCommand(this));
            commands.Add(new WowJokeCommand(this));
            commands.Add(new CalcCommand(this));
            commands.Add(new OsuCommands(this));
            commands.Add(new PokemonSearchCommands(this));
            commands.Add(new MemegenCommands(this));
            rng = new Random();
        }

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Searches;

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {

                cgb.AddCheck(PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(Prefix + "we")
                    .Description($"Shows weather data for a specified city and a country. BOTH ARE REQUIRED. Use country abbrevations. | `{Prefix}we Moscow RF`")
                    .Parameter("city", ParameterType.Required)
                    .Parameter("country", ParameterType.Required)
                    .Do(async e =>
                    {
                        var city = e.GetArg("city").Replace(" ", "");
                        var country = e.GetArg("country").Replace(" ", "");
                        var response = await SearchHelper.GetResponseStringAsync($"http://api.lawlypopzz.xyz/nadekobot/weather/?city={city}&country={country}").ConfigureAwait(false);

                        var obj = JObject.Parse(response)["weather"];

                        await e.Channel.SendMessage(
$@"🌍 **Weather for** 【{obj["target"]}】
📏 **Lat,Long:** ({obj["latitude"]}, {obj["longitude"]}) ☁ **Condition:** {obj["condition"]}
😓 **Humidity:** {obj["humidity"]}% 💨 **Wind Speed:** {obj["windspeedk"]}km/h / {obj["windspeedm"]}mph 
🔆 **Temperature:** {obj["centigrade"]}°C / {obj["fahrenheit"]}°F 🔆 **Feels like:** {obj["feelscentigrade"]}°C / {obj["feelsfahrenheit"]}°F
🌄 **Sunrise:** {obj["sunrise"]} 🌇 **Sunset:** {obj["sunset"]}").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "yt")
                    .Parameter("query", ParameterType.Unparsed)
                    .Description($"Searches youtubes and shows the first result | `{Prefix}yt query`")
                    .Do(async e =>
                    {
                        if (!(await SearchHelper.ValidateQuery(e.Channel, e.GetArg("query")).ConfigureAwait(false))) return;
                        var link = await SearchHelper.FindYoutubeUrlByKeywords(e.GetArg("query")).ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(link))
                        {
                            await e.Channel.SendMessage("No results found for that query.");
                            return;
                        }
                        var shortUrl = await SearchHelper.ShortenUrl(link).ConfigureAwait(false);
                        await e.Channel.SendMessage(shortUrl).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "ani")
                    .Alias(Prefix + "anime", Prefix + "aq")
                    .Parameter("query", ParameterType.Unparsed)
                    .Description($"Queries anilist for an anime and shows the first result. | `{Prefix}aq aquerion evol`")
                    .Do(async e =>
                    {
                        if (!(await SearchHelper.ValidateQuery(e.Channel, e.GetArg("query")).ConfigureAwait(false))) return;
                        string result;
                        try
                        {
                            result = (await SearchHelper.GetAnimeData(e.GetArg("query")).ConfigureAwait(false)).ToString();
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Failed to find that anime.").ConfigureAwait(false);
                            return;
                        }

                        await e.Channel.SendMessage(result.ToString()).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "imdb")
                    .Parameter("query", ParameterType.Unparsed)
                    .Description($"Queries imdb for movies or series, show first result. | `{Prefix}imdb query`")
                    .Do(async e =>
                    {
                        if (!(await SearchHelper.ValidateQuery(e.Channel, e.GetArg("query")).ConfigureAwait(false))) return;
                        await e.Channel.SendIsTyping().ConfigureAwait(false);
                        string result;
                        try
                        {
                            var movie = ImdbScraper.ImdbScrape(e.GetArg("query"), true);
                            if (movie.Status) result = movie.ToString();
                            else result = "Failed to find that movie.";
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Failed to find that movie.").ConfigureAwait(false);
                            return;
                        }

                        await e.Channel.SendMessage(result.ToString()).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "mang")
                    .Alias(Prefix + "manga").Alias(Prefix + "mq")
                    .Parameter("query", ParameterType.Unparsed)
                    .Description($"Queries anilist for a manga and shows the first result. | `{Prefix}mq query`")
                    .Do(async e =>
                    {
                        if (!(await SearchHelper.ValidateQuery(e.Channel, e.GetArg("query")).ConfigureAwait(false))) return;
                        string result;
                        try
                        {
                            result = (await SearchHelper.GetMangaData(e.GetArg("query")).ConfigureAwait(false)).ToString();
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Failed to find that anime.").ConfigureAwait(false);
                            return;
                        }
                        await e.Channel.SendMessage(result).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "randomcat")
                    .Alias(Prefix + "meow")
                    .Description("Shows a random cat image.")
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(JObject.Parse(
                            await SearchHelper.GetResponseStringAsync("http://www.random.cat/meow").ConfigureAwait(false))["file"].ToString())
                                .ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "randomdog")
                    .Alias(Prefix + "woof")
                    .Description("Shows a random dog image.")
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage("http://random.dog/" + await SearchHelper.GetResponseStringAsync("http://random.dog/woof").ConfigureAwait(false)).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "i")
                   .Description($"Pulls the first image found using a search parameter. Use ~ir for different results. | `{Prefix}i cute kitten`")
                   .Parameter("query", ParameterType.Unparsed)
                       .Do(async e =>
                       {
                           if (string.IsNullOrWhiteSpace(e.GetArg("query")))
                               return;
                           try
                           {
                               var reqString = $"https://www.googleapis.com/customsearch/v1?q={Uri.EscapeDataString(e.GetArg("query"))}&cx=018084019232060951019%3Ahs5piey28-e&num=1&searchType=image&fields=items%2Flink&key={NadekoBot.Creds.GoogleAPIKey}";
                               var obj = JObject.Parse(await SearchHelper.GetResponseStringAsync(reqString).ConfigureAwait(false));
                               await e.Channel.SendMessage(obj["items"][0]["link"].ToString()).ConfigureAwait(false);
                           }
                           catch (HttpRequestException exception)
                           {
                               if (exception.Message.Contains("403 (Forbidden)"))
                               {
                                   await e.Channel.SendMessage("Daily limit reached!");
                               }
                               else
                               {
                                   await e.Channel.SendMessage("Something went wrong.");
                               }
                           }
                       });

                cgb.CreateCommand(Prefix + "ir")
                   .Description($"Pulls a random image using a search parameter. | `{Prefix}ir cute kitten`")
                   .Parameter("query", ParameterType.Unparsed)
                   .Do(async e =>
                    {
                        if (string.IsNullOrWhiteSpace(e.GetArg("query")))
                            return;
                        try
                        {
                            var reqString = $"https://www.googleapis.com/customsearch/v1?q={Uri.EscapeDataString(e.GetArg("query"))}&cx=018084019232060951019%3Ahs5piey28-e&num=1&searchType=image&start={ rng.Next(1, 50) }&fields=items%2Flink&key={NadekoBot.Creds.GoogleAPIKey}";
                            var obj = JObject.Parse(await SearchHelper.GetResponseStringAsync(reqString).ConfigureAwait(false));
                            var items = obj["items"] as JArray;
                            await e.Channel.SendMessage(items[0]["link"].ToString()).ConfigureAwait(false);
                        }
                        catch (HttpRequestException exception)
                        {
                            if (exception.Message.Contains("403 (Forbidden)"))
                            {
                                await e.Channel.SendMessage("Daily limit reached!");
                            }
                            else
                            {
                                await e.Channel.SendMessage("Something went wrong.");
                            }
                        }
                    });

                cgb.CreateCommand(Prefix + "lmgtfy")
                    .Description($"Google something for an idiot. | `{Prefix}lmgtfy query`")
                    .Parameter("ffs", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        if (e.GetArg("ffs") == null || e.GetArg("ffs").Length < 1) return;
                        await e.Channel.SendMessage(await $"http://lmgtfy.com/?q={ Uri.EscapeUriString(e.GetArg("ffs").ToString()) }".ShortenUrl())
                                       .ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "google")
                    .Alias(Prefix + "g")
                    .Description($"Get a google search link for some terms. | `{Prefix}google query`")
                    .Parameter("terms", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var terms = e.GetArg("terms")?.Trim();
                        if (string.IsNullOrWhiteSpace(terms))
                            return;
                        await e.Channel.SendMessage($"https://google.com/search?q={ HttpUtility.UrlEncode(terms).Replace(' ', '+') }")
                                       .ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "hs")
                  .Description($"Searches for a Hearthstone card and shows its image. Takes a while to complete. | `{Prefix}hs Ysera`")
                  .Parameter("name", ParameterType.Unparsed)
                  .Do(async e =>
                  {
                      var arg = e.GetArg("name");
                      if (string.IsNullOrWhiteSpace(arg))
                      {
                          await e.Channel.SendMessage("💢 Please enter a card name to search for.").ConfigureAwait(false);
                          return;
                      }
                      await e.Channel.SendIsTyping().ConfigureAwait(false);
                      var headers = new Dictionary<string, string> { { "X-Mashape-Key", NadekoBot.Creds.MashapeKey } };
                      var res = await SearchHelper.GetResponseStringAsync($"https://omgvamp-hearthstone-v1.p.mashape.com/cards/search/{Uri.EscapeUriString(arg)}", headers)
                                                  .ConfigureAwait(false);
                      try
                      {
                          var items = JArray.Parse(res);
                          var images = new List<Image>();
                          if (items == null)
                              throw new KeyNotFoundException("Cannot find a card by that name");
                          var cnt = 0;
                          items.Shuffle();
                          foreach (var item in items.TakeWhile(item => cnt++ < 4).Where(item => item.HasValues && item["img"] != null))
                          {
                              images.Add(
                                  Image.FromStream(await SearchHelper.GetResponseStreamAsync(item["img"].ToString()).ConfigureAwait(false)));
                          }
                          if (items.Count > 4)
                          {
                              await e.Channel.SendMessage("⚠ Found over 4 images. Showing random 4.").ConfigureAwait(false);
                          }
                          await e.Channel.SendFile(arg + ".png", (await images.MergeAsync()).ToStream(System.Drawing.Imaging.ImageFormat.Png))
                                         .ConfigureAwait(false);
                      }
                      catch (Exception ex)
                      {
                          await e.Channel.SendMessage($"💢 Error {ex.Message}").ConfigureAwait(false);
                      }
                  });

                cgb.CreateCommand(Prefix + "ud")
                  .Description($"Searches Urban Dictionary for a word. | `{Prefix}ud Pineapple`")
                  .Parameter("query", ParameterType.Unparsed)
                  .Do(async e =>
                  {
                      var arg = e.GetArg("query");
                      if (string.IsNullOrWhiteSpace(arg))
                      {
                          await e.Channel.SendMessage("💢 Please enter a search term.").ConfigureAwait(false);
                          return;
                      }
                      await e.Channel.SendIsTyping().ConfigureAwait(false);
                      var headers = new Dictionary<string, string> { { "X-Mashape-Key", NadekoBot.Creds.MashapeKey } };
                      var res = await SearchHelper.GetResponseStringAsync($"https://mashape-community-urban-dictionary.p.mashape.com/define?term={Uri.EscapeUriString(arg)}", headers).ConfigureAwait(false);
                      try
                      {
                          var items = JObject.Parse(res);
                          var sb = new System.Text.StringBuilder();
                          sb.AppendLine($"`Term:` {items["list"][0]["word"].ToString()}");
                          sb.AppendLine($"`Definition:` {items["list"][0]["definition"].ToString()}");
                          sb.Append($"`Link:` <{await items["list"][0]["permalink"].ToString().ShortenUrl().ConfigureAwait(false)}>");
                          await e.Channel.SendMessage(sb.ToString());
                      }
                      catch
                      {
                          await e.Channel.SendMessage("💢 Failed finding a definition for that term.").ConfigureAwait(false);
                      }
                  });
                // thanks to Blaubeerwald
                cgb.CreateCommand(Prefix + "#")
                 .Description($"Searches Tagdef.com for a hashtag. | `{Prefix}# ff`")
                  .Parameter("query", ParameterType.Unparsed)
                  .Do(async e =>
                  {
                      var arg = e.GetArg("query");
                      if (string.IsNullOrWhiteSpace(arg))
                      {
                          await e.Channel.SendMessage("💢 Please enter a search term.").ConfigureAwait(false);
                          return;
                      }
                      await e.Channel.SendIsTyping().ConfigureAwait(false);
                      var headers = new Dictionary<string, string> { { "X-Mashape-Key", NadekoBot.Creds.MashapeKey } };
                      var res = await SearchHelper.GetResponseStringAsync($"https://tagdef.p.mashape.com/one.{Uri.EscapeUriString(arg)}.json", headers).ConfigureAwait(false);
                      try
                      {
                          var items = JObject.Parse(res);
                          var sb = new System.Text.StringBuilder();
                          sb.AppendLine($"`Hashtag:` {items["defs"]["def"]["hashtag"].ToString()}");
                          sb.AppendLine($"`Definition:` {items["defs"]["def"]["text"].ToString()}");
                          sb.Append($"`Link:` <{await items["defs"]["def"]["uri"].ToString().ShortenUrl().ConfigureAwait(false)}>");
                          await e.Channel.SendMessage(sb.ToString());
                      }
                      catch
                      {
                          await e.Channel.SendMessage("💢 Failed finidng a definition for that tag.").ConfigureAwait(false);
                      }
                  });

                cgb.CreateCommand(Prefix + "quote")
                    .Description($"Shows a random quote. | `{Prefix}quote`")
                    .Do(async e =>
                    {
                        var quote = NadekoBot.Config.Quotes[rng.Next(0, NadekoBot.Config.Quotes.Count)].ToString();
                        await e.Channel.SendMessage(quote).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "catfact")
                    .Description($"Shows a random catfact from <http://catfacts-api.appspot.com/api/facts> | `{Prefix}catfact`")
                    .Do(async e =>
                    {
                        var response = await SearchHelper.GetResponseStringAsync("http://catfacts-api.appspot.com/api/facts").ConfigureAwait(false);
                        if (response == null)
                            return;
                        await e.Channel.SendMessage($"🐈 `{JObject.Parse(response)["facts"][0].ToString()}`").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "yomama")
                    .Alias(Prefix + "ym")
                    .Description($"Shows a random joke from <http://api.yomomma.info/> | `{Prefix}ym`")
                    .Do(async e =>
                    {
                        var response = await SearchHelper.GetResponseStringAsync("http://api.yomomma.info/").ConfigureAwait(false);
                        await e.Channel.SendMessage("`" + JObject.Parse(response)["joke"].ToString() + "` 😆").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "randjoke")
                    .Alias(Prefix + "rj")
                    .Description($"Shows a random joke from <http://tambal.azurewebsites.net/joke/random> | `{Prefix}rj`")
                    .Do(async e =>
                    {
                        var response = await SearchHelper.GetResponseStringAsync("http://tambal.azurewebsites.net/joke/random").ConfigureAwait(false);
                        await e.Channel.SendMessage("`" + JObject.Parse(response)["joke"].ToString() + "` 😆").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "chucknorris")
                    .Alias(Prefix + "cn")
                    .Description($"Shows a random chucknorris joke from <http://tambal.azurewebsites.net/joke/random> | `{Prefix}cn`")
                    .Do(async e =>
                    {
                        var response = await SearchHelper.GetResponseStringAsync("http://api.icndb.com/jokes/random/").ConfigureAwait(false);
                        await e.Channel.SendMessage("`" + JObject.Parse(response)["value"]["joke"].ToString() + "` 😆").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "magicitem")
                    .Alias(Prefix + "mi")
                    .Description($"Shows a random magicitem from <https://1d4chan.org/wiki/List_of_/tg/%27s_magic_items> | `{Prefix}mi`")
                    .Do(async e =>
                    {
                        var magicItems = JsonConvert.DeserializeObject<List<MagicItem>>(File.ReadAllText("data/magicitems.json"));
                        var item = magicItems[rng.Next(0, magicItems.Count)].ToString();

                        await e.Channel.SendMessage(item).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "revav")
                    .Description($"Returns a google reverse image search for someone's avatar. | `{Prefix}revav \"@SomeGuy\"`")
                    .Parameter("user", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var usrStr = e.GetArg("user")?.Trim();

                        if (string.IsNullOrWhiteSpace(usrStr))
                            return;

                        var usr = e.Server.FindUsers(usrStr).FirstOrDefault();

                        if (usr == null || string.IsNullOrWhiteSpace(usr.AvatarUrl))
                            return;
                        await e.Channel.SendMessage($"https://images.google.com/searchbyimage?image_url={usr.AvatarUrl}").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "revimg")
                    .Description($"Returns a google reverse image search for an image from a link. | `{Prefix}revav Image link`")
                    .Parameter("image", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var imgLink = e.GetArg("image")?.Trim();

                        if (string.IsNullOrWhiteSpace(imgLink))
                            return;
                        await e.Channel.SendMessage($"https://images.google.com/searchbyimage?image_url={imgLink}").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "safebooru")
                    .Description($"Shows a random image from safebooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) | `{Prefix}safebooru yuri+kissing`")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var tag = e.GetArg("tag")?.Trim() ?? "";
                        var link = await SearchHelper.GetSafebooruImageLink(tag).ConfigureAwait(false);
                        if (link == null)
                            await e.Channel.SendMessage("`No results.`");
                        else
                            await e.Channel.SendMessage(link).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "wiki")
                    .Description($"Gives you back a wikipedia link | `{Prefix}wiki query`")
                    .Parameter("query", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var query = e.GetArg("query");
                        var result = await SearchHelper.GetResponseStringAsync("https://en.wikipedia.org//w/api.php?action=query&format=json&prop=info&redirects=1&formatversion=2&inprop=url&titles=" + Uri.EscapeDataString(query));
                        var data = JsonConvert.DeserializeObject<WikipediaApiModel>(result);
                        if (data.Query.Pages[0].Missing)
                            await e.Channel.SendMessage("`That page could not be found.`");
                        else
                            await e.Channel.SendMessage(data.Query.Pages[0].FullUrl);
                    });

                cgb.CreateCommand(Prefix + "clr")
                    .Description($"Shows you what color corresponds to that hex. | `{Prefix}clr 00ff00`")
                    .Parameter("color", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var arg1 = e.GetArg("color")?.Trim()?.Replace("#", "");
                        if (string.IsNullOrWhiteSpace(arg1))
                            return;
                        var img = new Bitmap(50, 50);

                        var red = Convert.ToInt32(arg1.Substring(0, 2), 16);
                        var green = Convert.ToInt32(arg1.Substring(2, 2), 16);
                        var blue = Convert.ToInt32(arg1.Substring(4, 2), 16);
                        var brush = new SolidBrush(System.Drawing.Color.FromArgb(red, green, blue));

                        using (Graphics g = Graphics.FromImage(img))
                        {
                            g.FillRectangle(brush, 0, 0, 50, 50);
                            g.Flush();
                        }

                        await e.Channel.SendFile("arg1.png", img.ToStream());
                    });


                cgb.CreateCommand(Prefix + "videocall")
                  .Description($"Creates a private <http://www.appear.in> video call link for you and other mentioned people. The link is sent to mentioned people via a private message. | `{Prefix}videocall \"@SomeGuy\"`")
                  .Parameter("arg", ParameterType.Unparsed)
                  .Do(async e =>
                  {
                      try
                      {
                          var allUsrs = e.Message.MentionedUsers.Union(new User[] { e.User });
                          var allUsrsArray = allUsrs as User[] ?? allUsrs.ToArray();
                          var str = allUsrsArray.Aggregate("http://appear.in/", (current, usr) => current + Uri.EscapeUriString(usr.Name[0].ToString()));
                          str += new Random().Next();
                          foreach (var usr in allUsrsArray)
                          {
                              await usr.SendMessage(str).ConfigureAwait(false);
                          }
                      }
                      catch (Exception ex)
                      {
                          Console.WriteLine(ex);
                      }
                  });

                cgb.CreateCommand(Prefix + "av")
                    .Alias(Prefix + "avatar")
                    .Parameter("mention", ParameterType.Required)
                    .Description($"Shows a mentioned person's avatar. | `{Prefix}av @X`")
                    .Do(async e =>
                    {
                        var usr = e.Channel.FindUsers(e.GetArg("mention")).FirstOrDefault();
                        if (usr == null)
                        {
                            await e.Channel.SendMessage("Invalid user specified.").ConfigureAwait(false);
                            return;
                        }
                        await e.Channel.SendMessage(await usr.AvatarUrl.ShortenUrl()).ConfigureAwait(false);
                    });

            });
        }
    }
}

