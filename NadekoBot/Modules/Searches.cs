using System;
using System.Threading.Tasks;
using Discord.Modules;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Discord.Commands;
using NadekoBot.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace NadekoBot.Modules {
    class Searches : DiscordModule {
        private Random _r;
        public Searches() : base() {
            // commands.Add(new OsuCommands());
            _r = new Random();
        }

        public override void Install(ModuleManager manager) {
            var client = NadekoBot.client;

            manager.CreateCommands("", cgb => {

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand("~yt")
                    .Parameter("query", ParameterType.Unparsed)
                    .Description("Searches youtubes and shows the first result")
                    .Do(async e => {
                        if (!(await ValidateQuery(e.Channel, e.GetArg("query")))) return;

                        var str = await ShortenUrl(await FindYoutubeUrlByKeywords(e.GetArg("query")));
                        if (string.IsNullOrEmpty(str.Trim())) {
                            await e.Send("Query failed");
                            return;
                        }
                        await e.Send(str);
                    });

                cgb.CreateCommand("~ani")
                    .Alias("~anime").Alias("~aq")
                    .Parameter("query", ParameterType.Unparsed)
                    .Description("Queries anilist for an anime and shows the first result.")
                    .Do(async e => {
                        if (!(await ValidateQuery(e.Channel, e.GetArg("query")))) return;

                        var result = await GetAnimeQueryResultLink(e.GetArg("query"));
                        if (result == null) {
                            await e.Send("Failed to find that anime.");
                            return;
                        }

                        await e.Send(result.ToString());
                    });

                cgb.CreateCommand("~mang")
                    .Alias("~manga").Alias("~mq")
                    .Parameter("query", ParameterType.Unparsed)
                    .Description("Queries anilist for a manga and shows the first result.")
                    .Do(async e => {
                        if (!(await ValidateQuery(e.Channel, e.GetArg("query")))) return;

                        var result = await GetMangaQueryResultLink(e.GetArg("query"));
                        if (result == null) {
                            await e.Send("Failed to find that anime.");
                            return;
                        }
                        await e.Send(result.ToString());
                    });

                cgb.CreateCommand("~randomcat")
                    .Description("Shows a random cat image.")
                    .Do(async e => {
                        try {
                            await e.Send(JObject.Parse(new StreamReader(
                                WebRequest.Create("http://www.random.cat/meow")
                                    .GetResponse()
                                    .GetResponseStream())
                                .ReadToEnd())["file"].ToString());
                        } catch (Exception) { }
                    });

                cgb.CreateCommand("~i")
                   .Description("Pulls a first image using a search parameter.\n**Usage**: @NadekoBot img Multiword_search_parameter")
                   .Alias("img")
                   .Parameter("all", ParameterType.Unparsed)
                       .Do(async e => {
                           await e.Send("This feature is being reconstructed.");

                       });

                cgb.CreateCommand("~ir")
                    .Description("Pulls a random image using a search parameter.\n**Usage**: @NadekoBot img Multiword_search_parameter")
                    .Alias("imgrandom")
                    .Parameter("all", ParameterType.Unparsed)
                    .Do(async e => {
                        await e.Send("This feature is being reconstructed.");

                    });

                cgb.CreateCommand("~hentai")
                    .Description("Shows a random NSFW hentai image from gelbooru and danbooru with a given tag. Tag is optional but preffered.\n**Usage**: ~hentai yuri")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        string tag = e.GetArg("tag");
                        if (tag == null)
                            tag = "";
                        await e.Send(":heart: Gelbooru: " + await GetGelbooruImageLink(tag));
                        await e.Send(":heart: Danbooru: " + await GetDanbooruImageLink(tag));
                    });
                cgb.CreateCommand("~danbooru")
                    .Description("Shows a random hentai image from danbooru with a given tag. Tag is optional but preffered.\n**Usage**: ~hentai yuri")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        string tag = e.GetArg("tag");
                        if (tag == null)
                            tag = "";
                        await e.Send(await GetDanbooruImageLink(tag));
                    });
                cgb.CreateCommand("~gelbooru")
                    .Description("Shows a random hentai image from gelbooru with a given tag. Tag is optional but preffered.\n**Usage**: ~hentai yuri")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        string tag = e.GetArg("tag");
                        if (tag == null)
                            tag = "";
                        await e.Send(await GetGelbooruImageLink(tag));
                    });
                cgb.CreateCommand("~cp")
                    .Description("We all know where this will lead you to.")
                    .Parameter("anything", ParameterType.Unparsed)
                    .Do(async e => {
                        await e.Send("http://i.imgur.com/MZkY1md.jpg");
                    });
                cgb.CreateCommand("lmgtfy")
                    .Alias("~lmgtfy")
                    .Description("Google something for an idiot.")
                    .Parameter("ffs", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.GetArg("ffs") == null || e.GetArg("ffs").Length < 1) return;
                        await e.Send(await $"http://lmgtfy.com/?q={ Uri.EscapeUriString(e.GetArg("ffs").ToString()) }".ShortenUrl());
                    });

                cgb.CreateCommand("~hs")
                  .Description("Searches for a Hearthstone card and shows its image.")
                  .Parameter("name", ParameterType.Unparsed)
                  .Do(async e => {
                      var arg = e.GetArg("name");
                      if (string.IsNullOrWhiteSpace(arg)) {
                          await e.Send(":anger: Please enter a card name to search for.");
                          return;
                      }
                      await e.Channel.SendIsTyping();
                      var res = await GetResponseAsync($"https://omgvamp-hearthstone-v1.p.mashape.com/cards/search/{Uri.EscapeUriString(arg)}",
                          new Tuple<string, string>[] {
                              new Tuple<string, string>("X-Mashape-Key", NadekoBot.creds.MashapeKey),
                          });
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
                              images.Add(System.Drawing.Bitmap.FromStream(await GetResponseStream(item["img"].ToString())));
                          }
                          if (items.Count > 4) {
                              await e.Send(":exclamation: Found over 4 images. Showing random 4.");
                          }
                          Console.WriteLine("Start");
                          await e.Channel.SendFile(arg + ".png", (await images.MergeAsync()).ToStream(System.Drawing.Imaging.ImageFormat.Png));
                          Console.WriteLine("Finish");
                      } catch (Exception ex) {
                          await e.Send($":anger: Error {ex}");
                      }
                  });
                /*
                cgb.CreateCommand("~osu")
                  .Description("desc")
                  .Parameter("arg", ParameterType.Required)
                  .Do(async e => {
                      var arg = e.GetArg("arg");
                      var res = await GetResponseStream($"http://lemmmy.pw/osusig/sig.php?uname=kwoth&flagshadow&xpbar&xpbarhex&pp=2");
                      await e.Channel.SendFile($"_{e.GetArg("arg")}.png", res);
                  });

                cgb.CreateCommand("~osubind")
                  .Description("Bind discord user to osu name\n**Usage**: ~osubind @MyDiscordName My osu name")
                  .Parameter("user_name", ParameterType.Required)
                  .Parameter("osu_name", ParameterType.Unparsed)
                  .Do(async e => {
                      var userName = e.GetArg("user_name");
                      var osuName = e.GetArg("osu_name");
                      var usr = e.Server.FindUsers(userName).FirstOrDefault();
                      if (usr == null) {
                          await e.Send("Cannot find that discord user.");
                          return;
                      }
                      //query for a username
                      //if exists save bind pair to parse.com
                      //if not valid error
                  });
                  */
            });
        }

        public static async Task<Stream> GetResponseStream(string v) {
            var wr = (HttpWebRequest)WebRequest.Create(v);
            try {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                wr.UserAgent = @"Mozilla/5.0 (Windows NT 6.2; Win64; x64)";
                return (await (wr).GetResponseAsync()).GetResponseStream();
            } catch (Exception ex) {
                Console.WriteLine("error in getresponse stream " + ex);
                return null;
            }
        }

        public static async Task<string> GetResponseAsync(string v) =>
            await new StreamReader((await ((HttpWebRequest)WebRequest.Create(v)).GetResponseAsync()).GetResponseStream()).ReadToEndAsync();

        public static async Task<string> GetResponseAsync(string v, IEnumerable<Tuple<string, string>> headers) {
            var wr = (HttpWebRequest)WebRequest.Create(v);
            foreach (var header in headers) {
                wr.Headers.Add(header.Item1, header.Item2);
            }
            return await new StreamReader((await wr.GetResponseAsync()).GetResponseStream()).ReadToEndAsync();
        }

        private string token = "";
        private async Task<AnimeResult> GetAnimeQueryResultLink(string query) {
            try {
                var cl = new RestSharp.RestClient("http://anilist.co/api");
                var rq = new RestSharp.RestRequest("/auth/access_token", RestSharp.Method.POST);

                RefreshAnilistToken();

                rq = new RestSharp.RestRequest("/anime/search/" + Uri.EscapeUriString(query));
                rq.AddParameter("access_token", token);

                var smallObj = JArray.Parse(cl.Execute(rq).Content)[0];

                rq = new RestSharp.RestRequest("anime/" + smallObj["id"]);
                rq.AddParameter("access_token", token);
                return await Task.Run(() => JsonConvert.DeserializeObject<AnimeResult>(cl.Execute(rq).Content));
            } catch (Exception) {
                return null;
            }
        }
        //todo kick out RestSharp and make it truly async
        private async Task<MangaResult> GetMangaQueryResultLink(string query) {
            try {
                RefreshAnilistToken();

                var cl = new RestSharp.RestClient("http://anilist.co/api");
                var rq = new RestSharp.RestRequest("/auth/access_token", RestSharp.Method.POST);
                rq = new RestSharp.RestRequest("/manga/search/" + Uri.EscapeUriString(query));
                rq.AddParameter("access_token", token);

                var smallObj = JArray.Parse(cl.Execute(rq).Content)[0];

                rq = new RestSharp.RestRequest("manga/" + smallObj["id"]);
                rq.AddParameter("access_token", token);
                return await Task.Run(() => JsonConvert.DeserializeObject<MangaResult>(cl.Execute(rq).Content));
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        private void RefreshAnilistToken() {
            try {
                var cl = new RestSharp.RestClient("http://anilist.co/api");
                var rq = new RestSharp.RestRequest("/auth/access_token", RestSharp.Method.POST);
                rq.AddParameter("grant_type", "client_credentials");
                rq.AddParameter("client_id", "kwoth-w0ki9");
                rq.AddParameter("client_secret", "Qd6j4FIAi1ZK6Pc7N7V4Z");
                var exec = cl.Execute(rq);
                /*
                Console.WriteLine($"Server gave me content: { exec.Content }\n{ exec.ResponseStatus } -> {exec.ErrorMessage} ");
                Console.WriteLine($"Err exception: {exec.ErrorException}");
                Console.WriteLine($"Inner: {exec.ErrorException.InnerException}");
                */

                token = JObject.Parse(exec.Content)["access_token"].ToString();
            } catch (Exception ex) {
                Console.WriteLine($"Failed refreshing anilist token:\n {ex}");
            }
        }

        private static async Task<bool> ValidateQuery(Discord.Channel ch, string query) {
            if (string.IsNullOrEmpty(query.Trim())) {
                await ch.Send("Please specify search parameters.");
                return false;
            }
            return true;
        }

        public static async Task<string> FindYoutubeUrlByKeywords(string v) {
            if (NadekoBot.GoogleAPIKey == "" || NadekoBot.GoogleAPIKey == null) {
                Console.WriteLine("ERROR: No google api key found. Playing `Never gonna give you up`.");
                return @"https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            }
            try {
                //maybe it is already a youtube url, in which case we will just extract the id and prepend it with youtube.com?v=
                var match = new Regex("(?:youtu\\.be\\/|v=)(?<id>[\\da-zA-Z\\-_]*)").Match(v);
                if (match.Length > 1) {
                    string str = $"http://www.youtube.com?v={ match.Groups["id"].Value }";
                    return str;
                }

                WebRequest wr = WebRequest.Create("https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&q=" + Uri.EscapeDataString(v) + "&key=" + NadekoBot.GoogleAPIKey);

                var sr = new StreamReader((await wr.GetResponseAsync()).GetResponseStream());

                dynamic obj = JObject.Parse(await sr.ReadToEndAsync());
                return "http://www.youtube.com/watch?v=" + obj.items[0].id.videoId.ToString();
            } catch (Exception ex) {
                Console.WriteLine($"Error in findyoutubeurl: {ex.Message}");
                return string.Empty;
            }
        }

        public static async Task<string> GetPlaylistIdByKeyword(string v) {
            if (NadekoBot.GoogleAPIKey == "" || NadekoBot.GoogleAPIKey == null) {
                Console.WriteLine("ERROR: No google api key found. Playing `Never gonna give you up`.");
                return string.Empty;
            }
            try {
                WebRequest wr = WebRequest.Create($"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&q={Uri.EscapeDataString(v)}&type=playlist&key={NadekoBot.creds.GoogleAPIKey}");

                var sr = new StreamReader((await wr.GetResponseAsync()).GetResponseStream());

                dynamic obj = JObject.Parse(await sr.ReadToEndAsync());
                return obj.items[0].id.playlistId.ToString();
            } catch (Exception ex) {
                Console.WriteLine($"Error in GetPlaylistId: {ex.Message}");
                return string.Empty;
            }
        }

        public static async Task<List<string>> GetVideoIDs(string v) {
            List<string> toReturn = new List<string>();
            if (NadekoBot.GoogleAPIKey == "" || NadekoBot.GoogleAPIKey == null) {
                Console.WriteLine("ERROR: No google api key found. Playing `Never gonna give you up`.");
                return toReturn;
            }
            try {

                WebRequest wr = WebRequest.Create($"https://www.googleapis.com/youtube/v3/playlistItems?part=contentDetails&maxResults={25}&playlistId={v}&key={ NadekoBot.creds.GoogleAPIKey }");

                var sr = new StreamReader((await wr.GetResponseAsync()).GetResponseStream());

                dynamic obj = JObject.Parse(await sr.ReadToEndAsync());

                foreach (var item in obj.items) {
                    toReturn.Add("http://www.youtube.com/watch?v=" + item.contentDetails.videoId);
                }
                return toReturn;
            } catch (Exception ex) {
                Console.WriteLine($"Error in GetPlaylistId: {ex.Message}");
                return new List<string>();
            }
        }


        public async Task<string> GetDanbooruImageLink(string tag) {
            try {
                var rng = new Random();

                if (tag == "loli") //loli doesn't work for some reason atm
                    tag = "flat_chest";

                var webpage = await GetResponseAsync($"http://danbooru.donmai.us/posts?page={ rng.Next(0, 30) }&tags={ tag.Replace(" ", "_") }");
                var matches = Regex.Matches(webpage, "data-large-file-url=\"(?<id>.*?)\"");

                return await $"http://danbooru.donmai.us{ matches[rng.Next(0, matches.Count)].Groups["id"].Value }".ShortenUrl();
            } catch (Exception) {
                return null;
            }
        }

        public async Task<string> GetGelbooruImageLink(string tag) {
            try {
                var rng = new Random();
                var url = $"http://gelbooru.com/index.php?page=post&s=list&pid={ rng.Next(0, 15) * 42 }&tags={ tag.Replace(" ", "_") }";
                var webpage = await GetResponseAsync(url); // first extract the post id and go to that posts page
                var matches = Regex.Matches(webpage, "span id=\"s(?<id>\\d*)\"");
                var postLink = $"http://gelbooru.com/index.php?page=post&s=view&id={ matches[rng.Next(0, matches.Count)].Groups["id"].Value }";
                webpage = await GetResponseAsync(postLink);
                //now extract the image from post page
                var match = Regex.Match(webpage, "\"(?<url>http://simg4.gelbooru.com//images.*?)\"");
                return match.Groups["url"].Value;
            } catch (Exception) {
                return null;
            }
        }

        public static async Task<string> ShortenUrl(string url) {
            if (NadekoBot.GoogleAPIKey == null || NadekoBot.GoogleAPIKey == "") return url;
            try {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/urlshortener/v1/url?key=" + NadekoBot.GoogleAPIKey);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(await httpWebRequest.GetRequestStreamAsync())) {
                    string json = "{\"longUrl\":\"" + url + "\"}";
                    streamWriter.Write(json);
                }

                var httpResponse = (await httpWebRequest.GetResponseAsync()) as HttpWebResponse;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    string responseText = await streamReader.ReadToEndAsync();
                    string MATCH_PATTERN = @"""id"": ?""(?<id>.+)""";
                    return Regex.Match(responseText, MATCH_PATTERN).Groups["id"].Value;
                }
            } catch (Exception ex) { Console.WriteLine(ex.ToString()); return ""; }
        }
    }
}
