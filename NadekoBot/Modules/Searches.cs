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

namespace NadekoBot.Modules
{
    class Searches : DiscordModule
    {
        public Searches() : base()
        {

        }

        public override void Install(ModuleManager manager)
        {
            var client = NadekoBot.client;

            manager.CreateCommands("",cgb =>
            {
                cgb.CreateCommand("~yt")
                    .Parameter("query",Discord.Commands.ParameterType.Unparsed)
                    .Description("Queries youtubes and embeds the first result")
                    .Do(async e =>
                    {
                        if (!(await ValidateQuery(e.Channel, e.GetArg("query")))) return;

                        var str = ShortenUrl(FindYoutubeUrlByKeywords(e.GetArg("query")));
                        if (string.IsNullOrEmpty(str.Trim()))
                        {
                            await e.Send( "Query failed");
                            return;
                        }
                        await e.Send( str);
                    });

                cgb.CreateCommand("~ani")
                    .Alias("~anime").Alias("~aq")
                    .Parameter("query", Discord.Commands.ParameterType.Unparsed)
                    .Description("Queries anilist for an anime and shows the first result.")
                    .Do(async e =>
                    {
                        if (!(await ValidateQuery(e.Channel, e.GetArg("query")))) return;

                        var result = GetAnimeQueryResultLink(e.GetArg("query"));
                        if (result == null) { 
                            await e.Send( "Failed to find that anime.");
                            return;
                        }

                        await e.Send(result.ToString());
                    });

                cgb.CreateCommand("~mang")
                    .Alias("~manga").Alias("~mq")
                    .Parameter("query", Discord.Commands.ParameterType.Unparsed)
                    .Description("Queries anilist for a manga and shows the first result.")
                    .Do(async e =>
                    {
                        if (!(await ValidateQuery(e.Channel, e.GetArg("query")))) return;

                        var result = GetMangaQueryResultLink(e.GetArg("query"));
                        if (result == null)
                        {
                            await e.Send( "Failed to find that anime.");
                            return;
                        }
                        await e.Send( result.ToString());
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
                        await e.Send(":heart: Gelbooru: " + GetGelbooruImageLink(tag));
                        await e.Send(":heart: Danbooru: " + GetDanbooruImageLink(tag));
                    });
                cgb.CreateCommand("~danbooru")
                    .Description("Shows a random hentai image from danbooru with a given tag. Tag is optional but preffered.\n**Usage**: ~hentai yuri")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        string tag = e.GetArg("tag");
                        if (tag == null)
                            tag = "";
                        await e.Send(GetDanbooruImageLink(tag));
                    });
                cgb.CreateCommand("~gelbooru")
                    .Description("Shows a random hentai image from gelbooru with a given tag. Tag is optional but preffered.\n**Usage**: ~hentai yuri")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        string tag = e.GetArg("tag");
                        if (tag == null)
                            tag = "";
                        await e.Send(GetGelbooruImageLink(tag));
                    });
                cgb.CreateCommand("lmgtfy")
                    .Description("Google something for an idiot.")
                    .Parameter("ffs", ParameterType.Unparsed)
                    .Do(async e => {
                        if (e.GetArg("ffs") == null || e.GetArg("ffs").Length < 1) return;
                        await e.Send($"http://lmgtfy.com/?q={ Uri.EscapeUriString(e.GetArg("ffs").ToString()) }".ShortenUrl());
                    });
            });
        }

        public static string MakeRequestAndGetResponse(string v) =>
            new StreamReader(((HttpWebRequest)WebRequest.Create(v)).GetResponse().GetResponseStream()).ReadToEnd();

        private string token = "";
        private AnimeResult GetAnimeQueryResultLink(string query)
        {
            try
            {
                var cl = new RestSharp.RestClient("http://anilist.co/api");
                var rq = new RestSharp.RestRequest("/auth/access_token", RestSharp.Method.POST);

                RefreshToken();

                rq = new RestSharp.RestRequest("/anime/search/" + Uri.EscapeUriString(query));
                rq.AddParameter("access_token", token);

                var smallObj = JArray.Parse(cl.Execute(rq).Content)[0];

                rq = new RestSharp.RestRequest("anime/" + smallObj["id"]);
                rq.AddParameter("access_token", token);
                return JsonConvert.DeserializeObject<AnimeResult>(cl.Execute(rq).Content);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private MangaResult GetMangaQueryResultLink(string query)
        {
            try
            {
                RefreshToken();

                var cl = new RestSharp.RestClient("http://anilist.co/api");
                var rq = new RestSharp.RestRequest("/auth/access_token", RestSharp.Method.POST);
                rq = new RestSharp.RestRequest("/manga/search/"+Uri.EscapeUriString(query));
                rq.AddParameter("access_token", token);
                
                var smallObj = JArray.Parse(cl.Execute(rq).Content)[0];

                rq = new RestSharp.RestRequest("manga/" + smallObj["id"]);
                rq.AddParameter("access_token", token);
                return JsonConvert.DeserializeObject<MangaResult> (cl.Execute(rq).Content);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        private void RefreshToken()
        {
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
        }

        private static async Task<bool> ValidateQuery(Discord.Channel ch,string query) {
            if (string.IsNullOrEmpty(query.Trim()))
            {
                await ch.Send("Please specify search parameters.");
                return false;
            }
            return true;
        }

        public static string FindYoutubeUrlByKeywords(string v) {
            if (NadekoBot.GoogleAPIKey == "" || NadekoBot.GoogleAPIKey == null) {
                Console.WriteLine("ERROR: No google api key found. Playing `Never gonna give you up`.");
                return @"https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            }
            WebRequest wr = WebRequest.Create("https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&q=" + Uri.EscapeDataString(v) + "&key=" + NadekoBot.GoogleAPIKey);

            var sr = new StreamReader(wr.GetResponse().GetResponseStream());

            dynamic obj = JObject.Parse(sr.ReadToEnd());
            string toReturn = "http://www.youtube.com/watch?v=" + obj.items[0].id.videoId.ToString();
            return toReturn;
        }

        public string GetDanbooruImageLink(string tag) {
            try {
                var rng = new Random();

                if (tag == "loli") //loli doesn't work for some reason atm
                    tag = "flat_chest";

                var webpage = MakeRequestAndGetResponse($"http://danbooru.donmai.us/posts?page={ rng.Next(0, 30) }&tags={ tag.Replace(" ","_") }");
                var matches = Regex.Matches(webpage, "data-large-file-url=\"(?<id>.*?)\"");

                return $"http://danbooru.donmai.us{ matches[rng.Next(0, matches.Count)].Groups["id"].Value }".ShortenUrl();
            } catch (Exception) {
                return null;
            }
        }

        public string GetGelbooruImageLink(string tag) {
            try {
                var rng = new Random();
                var url = $"http://gelbooru.com/index.php?page=post&s=list&pid={ rng.Next(0, 15) * 42 }&tags={ tag.Replace(" ", "_") }";
                var webpage = MakeRequestAndGetResponse(url); // first extract the post id and go to that posts page
                var matches = Regex.Matches(webpage, "span id=\"s(?<id>\\d*)\"");
                var postLink = $"http://gelbooru.com/index.php?page=post&s=view&id={ matches[rng.Next(0, matches.Count)].Groups["id"].Value }";
                webpage = MakeRequestAndGetResponse(postLink);
                //now extract the image from post page
                var match = Regex.Match(webpage, "\"(?<url>http://simg4.gelbooru.com//images.*?)\"");
                return match.Groups["url"].Value;
            } catch (Exception) {
                return null;
            }
        }

        public static string ShortenUrl(string url)
        {
            if (NadekoBot.GoogleAPIKey == null || NadekoBot.GoogleAPIKey == "") return url;

            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/urlshortener/v1/url?key=" + NadekoBot.GoogleAPIKey);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = "{\"longUrl\":\"" + url + "\"}";
                streamWriter.Write(json);
            }
            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();
                    string MATCH_PATTERN = @"""id"": ?""(?<id>.+)""";
                    return Regex.Match(responseText, MATCH_PATTERN).Groups["id"].Value;
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); return ""; }
        }
    }
}
