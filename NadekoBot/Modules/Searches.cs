using System;
using Discord.Modules;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using Discord.Commands;
using NadekoBot.Extensions;
using System.Collections.Generic;
using NadekoBot.Classes;
using NadekoBot.Commands;

namespace NadekoBot.Modules {
    class Searches : DiscordModule {
        private Random _r;
        public Searches() : base() {
            //commands.Add(new LoLCommands());
            _r = new Random();
        }

        public override void Install(ModuleManager manager) {
            var client = NadekoBot.client;

            manager.CreateCommands("", cgb => {

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand("~yt")
                    .Parameter("query", ParameterType.Unparsed)
                    .Description("Searches youtubes and shows the first result")
                    .Do(async e => {
                        if (!(await SearchHelper.ValidateQuery(e.Channel, e.GetArg("query")))) return;

                        var str = await SearchHelper.ShortenUrl(await SearchHelper.FindYoutubeUrlByKeywords(e.GetArg("query")));
                        if (string.IsNullOrEmpty(str.Trim())) {
                            await e.Channel.SendMessage("Query failed");
                            return;
                        }
                        await e.Channel.SendMessage(str);
                    });

                cgb.CreateCommand("~ani")
                    .Alias("~anime").Alias("~aq")
                    .Parameter("query", ParameterType.Unparsed)
                    .Description("Queries anilist for an anime and shows the first result.")
                    .Do(async e => {
                        if (!(await SearchHelper.ValidateQuery(e.Channel, e.GetArg("query")))) return;

                        var result = await SearchHelper.GetAnimeQueryResultLink(e.GetArg("query"));
                        if (result == null) {
                            await e.Channel.SendMessage("Failed to find that anime.");
                            return;
                        }

                        await e.Channel.SendMessage(result.ToString());
                    });

                cgb.CreateCommand("~mang")
                    .Alias("~manga").Alias("~mq")
                    .Parameter("query", ParameterType.Unparsed)
                    .Description("Queries anilist for a manga and shows the first result.")
                    .Do(async e => {
                        if (!(await SearchHelper.ValidateQuery(e.Channel, e.GetArg("query")))) return;

                        var result = await SearchHelper.GetMangaQueryResultLink(e.GetArg("query"));
                        if (result == null) {
                            await e.Channel.SendMessage("Failed to find that anime.");
                            return;
                        }
                        await e.Channel.SendMessage(result.ToString());
                    });

                cgb.CreateCommand("~randomcat")
                    .Description("Shows a random cat image.")
                    .Do(async e => {
                        try {
                            await e.Channel.SendMessage(JObject.Parse(new StreamReader(
                                WebRequest.Create("http://www.random.cat/meow")
                                    .GetResponse()
                                    .GetResponseStream())
                                .ReadToEnd())["file"].ToString());
                        }
                        catch { }
                    });

                cgb.CreateCommand("~i")
                   .Description("Pulls a first image using a search parameter. Use ~ir for different results.\n**Usage**: ~i cute kitten")
                   .Parameter("query", ParameterType.Unparsed)
                       .Do(async e => {
                           if (string.IsNullOrWhiteSpace(e.GetArg("query")))
                               return;
                           try {
                               var reqString = $"https://www.googleapis.com/customsearch/v1?q={Uri.EscapeDataString(e.GetArg("query"))}&cx=018084019232060951019%3Ahs5piey28-e&num=1&searchType=image&fields=items%2Flink&key={NadekoBot.creds.GoogleAPIKey}";
                               var obj = JObject.Parse(await SearchHelper.GetResponseAsync(reqString));
                               await e.Channel.SendMessage(obj["items"][0]["link"].ToString());
                           }
                           catch (Exception ex) {
                               await e.Channel.SendMessage($"💢 {ex.Message}");
                           }
                       });

                cgb.CreateCommand("~ir")
                   .Description("Pulls a random image using a search parameter.\n**Usage**: ~ir cute kitten")
                   .Parameter("query", ParameterType.Unparsed)
                       .Do(async e => {
                           if (string.IsNullOrWhiteSpace(e.GetArg("query")))
                               return;
                           try {
                               var reqString = $"https://www.googleapis.com/customsearch/v1?q={Uri.EscapeDataString(e.GetArg("query"))}&cx=018084019232060951019%3Ahs5piey28-e&num=1&searchType=image&start={ _r.Next(1, 150) }&fields=items%2Flink&key={NadekoBot.creds.GoogleAPIKey}";
                               var obj = JObject.Parse(await SearchHelper.GetResponseAsync(reqString));
                               await e.Channel.SendMessage(obj["items"][0]["link"].ToString());
                           }
                           catch (Exception ex) {
                               await e.Channel.SendMessage($"💢 {ex.Message}");
                           }
                       });
                cgb.CreateCommand("lmgtfy")
                    .Alias("~lmgtfy")
                    .Description("Google something for an idiot.")
                    .Parameter("ffs", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.GetArg("ffs") == null || e.GetArg("ffs").Length < 1) return;
                        await e.Channel.SendMessage(await $"http://lmgtfy.com/?q={ Uri.EscapeUriString(e.GetArg("ffs").ToString()) }".ShortenUrl());
                    });

                cgb.CreateCommand("~hs")
                  .Description("Searches for a Hearthstone card and shows its image. Takes a while to complete.\n**Usage**:~hs Ysera")
                  .Parameter("name", ParameterType.Unparsed)
                  .Do(async e => {
                      var arg = e.GetArg("name");
                      if (string.IsNullOrWhiteSpace(arg)) {
                          await e.Channel.SendMessage("💢 Please enter a card name to search for.");
                          return;
                      }
                      await e.Channel.SendIsTyping();
                      var headers = new WebHeaderCollection();
                      headers.Add("X-Mashape-Key", NadekoBot.creds.MashapeKey);
                      var res = await SearchHelper.GetResponseAsync($"https://omgvamp-hearthstone-v1.p.mashape.com/cards/search/{Uri.EscapeUriString(arg)}", headers);
                      try {
                          var items = JArray.Parse(res);
                          List<System.Drawing.Image> images = new List<System.Drawing.Image>();
                          if (items == null)
                              throw new KeyNotFoundException("Cannot find a card by that name");
                          int cnt = 0;
                          items.Shuffle();
                          foreach (var item in items) {
                              if (cnt >= 4)
                                  break;
                              if (!item.HasValues || item["img"] == null)
                                  continue;
                              cnt++;
                              images.Add(System.Drawing.Bitmap.FromStream(await SearchHelper.GetResponseStream(item["img"].ToString())));
                          }
                          if (items.Count > 4) {
                              await e.Channel.SendMessage("⚠ Found over 4 images. Showing random 4.");
                          }
                          Console.WriteLine("Start");
                          await e.Channel.SendFile(arg + ".png", (await images.MergeAsync()).ToStream(System.Drawing.Imaging.ImageFormat.Png));
                          Console.WriteLine("Finish");
                      }
                      catch (Exception ex) {
                          await e.Channel.SendMessage($"💢 Error {ex.Message}");
                      }
                  });

                cgb.CreateCommand("~osu")
                  .Description("Shows osu stats for a player\n**Usage**:~osu Name")
                  .Parameter("usr", ParameterType.Unparsed)
                  .Do(async e => {
                      if (string.IsNullOrWhiteSpace(e.GetArg("usr")))
                          return;

                      using (WebClient cl = new WebClient()) {
                          try {
                              cl.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                              cl.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.2; Win64; x64)");
                              cl.DownloadDataAsync(new Uri($"http://lemmmy.pw/osusig/sig.php?uname={ e.GetArg("usr") }&flagshadow&xpbar&xpbarhex&pp=2"));
                              cl.DownloadDataCompleted += async (s, cle) => {
                                  try {
                                      await e.Channel.SendFile($"{e.GetArg("usr")}.png", new MemoryStream(cle.Result));
                                      await e.Channel.SendMessage($"`Profile Link:`https://osu.ppy.sh/u/{Uri.EscapeDataString(e.GetArg("usr"))}\n`Image provided by https://lemmmy.pw/osusig`");
                                  }
                                  catch { }
                              };
                          }
                          catch {
                              await e.Channel.SendMessage("💢 Failed retrieving osu signature :\\");
                          }
                      }
                  });

                cgb.CreateCommand("~ud")
                  .Description("Searches Urban Dictionary for a word\n**Usage**:~ud Pineapple")
                  .Parameter("query", ParameterType.Unparsed)
                  .Do(async e => {
                      var arg = e.GetArg("query");
                      if (string.IsNullOrWhiteSpace(arg)) {
                          await e.Channel.SendMessage("💢 Please enter a search term.");
                          return;
                      }
                      await e.Channel.SendIsTyping();
                      var headers = new WebHeaderCollection();
                      headers.Add("X-Mashape-Key", NadekoBot.creds.MashapeKey);
                      var res = await SearchHelper.GetResponseAsync($"https://mashape-community-urban-dictionary.p.mashape.com/define?term={Uri.EscapeUriString(arg)}", headers);
                      try {
                          var items = JObject.Parse(res);
                          var sb = new System.Text.StringBuilder();
                          sb.AppendLine($"`Term:` {items["list"][0]["word"].ToString()}");
                          sb.AppendLine($"`Definition:` {items["list"][0]["definition"].ToString()}");
                          sb.Append($"`Link:` <{await items["list"][0]["permalink"].ToString().ShortenUrl()}>");
                          await e.Channel.SendMessage(sb.ToString());
                      }
                      catch {
                          await e.Channel.SendMessage("💢 Failed finding a definition for that term.");
                      }
                  });
                // thanks to Blaubeerwald
                cgb.CreateCommand("~#")
                 .Description("Searches Tagdef.com for a hashtag\n**Usage**:~# ff")
                  .Parameter("query", ParameterType.Unparsed)
                  .Do(async e => {
                      var arg = e.GetArg("query");
                      if (string.IsNullOrWhiteSpace(arg)) {
                          await e.Channel.SendMessage("💢 Please enter a search term.");
                          return;
                      }
                      await e.Channel.SendIsTyping();
                      var headers = new WebHeaderCollection();
                      headers.Add("X-Mashape-Key", NadekoBot.creds.MashapeKey);
                      var res = await SearchHelper.GetResponseAsync($"https://tagdef.p.mashape.com/one.{Uri.EscapeUriString(arg)}.json", headers);
                      try {
                          var items = JObject.Parse(res);
                          var sb = new System.Text.StringBuilder();
                          sb.AppendLine($"`Hashtag:` {items["defs"]["def"]["hashtag"].ToString()}");
                          sb.AppendLine($"`Definition:` {items["defs"]["def"]["text"].ToString()}");
                          sb.Append($"`Link:` <{await items["defs"]["def"]["uri"].ToString().ShortenUrl()}>");
                          await e.Channel.SendMessage(sb.ToString());
                      }
                      catch {
                          await e.Channel.SendMessage("💢 Failed finidng a definition for that tag");
                      }
                  });
                //todo when moved from parse
                /*
                cgb.CreateCommand("~osubind")
                    .Description("Bind discord user to osu name\n**Usage**: ~osubind My osu name")
                    .Parameter("osu_name", ParameterType.Unparsed)
                    .Do(async e => {
                        var userName = e.GetArg("user_name");
                        var osuName = e.GetArg("osu_name");
                        var usr = e.Server.FindUsers(userName).FirstOrDefault();
                        if (usr == null) {
                            await e.Channel.SendMessage("Cannot find that discord user.");
                            return;
                        }
                    });
                */
            });
        }
    }
}
