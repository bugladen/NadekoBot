using NadekoBot.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NadekoBot.Classes.JSONModels;

namespace NadekoBot.Classes {
    public enum RequestHttpMethod {
        Get,
        Post
    }

    public static class SearchHelper {
        private static DateTime lastRefreshed = DateTime.MinValue;
        private static string token { get; set; } = "";

        public static async Task<Stream> GetResponseStreamAsync(string url,
            IEnumerable<KeyValuePair<string, string>> headers = null, RequestHttpMethod method = RequestHttpMethod.Get) {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));
            var httpClient = new HttpClient();
            switch (method) {
                case RequestHttpMethod.Get:
                    if (headers != null) {
                        foreach (var header in headers) {
                            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                    return await httpClient.GetStreamAsync(url);
                case RequestHttpMethod.Post:
                    FormUrlEncodedContent formContent = null;
                    if (headers != null) {
                        formContent = new FormUrlEncodedContent(headers);
                    }
                    var message = await httpClient.PostAsync(url, formContent);
                    return await message.Content.ReadAsStreamAsync();
                default:
                    throw new NotImplementedException("That type of request is unsupported.");
            }
        }

        public static async Task<string> GetResponseStringAsync(string url,
            IEnumerable<KeyValuePair<string, string>> headers = null,
            RequestHttpMethod method = RequestHttpMethod.Get) {

            using (var streamReader = new StreamReader(await GetResponseStreamAsync(url, headers, method))) {
                return await streamReader.ReadToEndAsync();
            }
        }

        public static async Task<AnimeResult> GetAnimeData(string query) {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            await RefreshAnilistToken();

            var link = "http://anilist.co/api/anime/search/" + Uri.EscapeUriString(query);
            var smallContent = "";
            var cl = new RestSharp.RestClient("http://anilist.co/api");
            var rq = new RestSharp.RestRequest("/anime/search/" + Uri.EscapeUriString(query));
            rq.AddParameter("access_token", token);
            smallContent = cl.Execute(rq).Content;
            var smallObj = JArray.Parse(smallContent)[0];

            rq = new RestSharp.RestRequest("/anime/" + smallObj["id"]);
            rq.AddParameter("access_token", token);
            var content = cl.Execute(rq).Content;

            return await Task.Run(() => JsonConvert.DeserializeObject<AnimeResult>(content));
        }

        public static async Task<MangaResult> GetMangaData(string query) {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            await RefreshAnilistToken();

            var link = "http://anilist.co/api/anime/search/" + Uri.EscapeUriString(query);
            var smallContent = "";
            var cl = new RestSharp.RestClient("http://anilist.co/api");
            var rq = new RestSharp.RestRequest("/manga/search/" + Uri.EscapeUriString(query));
            rq.AddParameter("access_token", token);
            smallContent = cl.Execute(rq).Content;
            var smallObj = JArray.Parse(smallContent)[0];

            rq = new RestSharp.RestRequest("/manga/" + smallObj["id"]);
            rq.AddParameter("access_token", token);
            var content = cl.Execute(rq).Content;

            return await Task.Run(() => JsonConvert.DeserializeObject<MangaResult>(content));
        }

        private static async Task RefreshAnilistToken() {
            if (DateTime.Now - lastRefreshed > TimeSpan.FromMinutes(29))
                lastRefreshed = DateTime.Now;
            else {
                return;
            }
            var headers = new Dictionary<string, string> {
                {"grant_type", "client_credentials"},
                {"client_id", "kwoth-w0ki9"},
                {"client_secret", "Qd6j4FIAi1ZK6Pc7N7V4Z"},
            };
            var content =
                await GetResponseStringAsync("http://anilist.co/api/auth/access_token", headers, RequestHttpMethod.Post);

            token = JObject.Parse(content)["access_token"].ToString();
        }

        public static async Task<bool> ValidateQuery(Discord.Channel ch, string query) {
            if (!string.IsNullOrEmpty(query.Trim())) return true;
            await ch.Send("Please specify search parameters.");
            return false;
        }

        public static async Task<string> FindYoutubeUrlByKeywords(string keywords) {
            if (string.IsNullOrWhiteSpace(NadekoBot.Creds.GoogleAPIKey))
                throw new InvalidCredentialException("Google API Key is missing.");
            if (string.IsNullOrWhiteSpace(keywords))
                throw new ArgumentNullException(nameof(keywords), "Query not specified.");
            if (keywords.Length > 150)
                throw new ArgumentException("Query is too long.");

            //maybe it is already a youtube url, in which case we will just extract the id and prepend it with youtube.com?v=
            var match = new Regex("(?:youtu\\.be\\/|v=)(?<id>[\\da-zA-Z\\-_]*)").Match(keywords);
            if (match.Length > 1) {
                return $"http://www.youtube.com?v={match.Groups["id"].Value}";
            }
            var response =
                await
                    GetResponseStringAsync($"https://www.googleapis.com/youtube/v3/search?" +
                                           $"part=snippet&maxResults=1" +
                                           $"&q={Uri.EscapeDataString(keywords)}" +
                                           $"&key={NadekoBot.Creds.GoogleAPIKey}");
            dynamic obj = JObject.Parse(response);
            return "http://www.youtube.com/watch?v=" + obj.items[0].id.videoId.ToString();
        }

        public static async Task<string> GetPlaylistIdByKeyword(string query) {
            if (string.IsNullOrWhiteSpace(NadekoBot.Creds.GoogleAPIKey))
                throw new ArgumentNullException(nameof(query));

            var link = "https://www.googleapis.com/youtube/v3/search?part=snippet" +
                        "&maxResults=1&type=playlist" +
                       $"&q={Uri.EscapeDataString(query)}" +
                       $"&key={NadekoBot.Creds.GoogleAPIKey}";

            var response = await GetResponseStringAsync(link);
            dynamic obj = JObject.Parse(response);

            return obj.items[0].id.playlistId.ToString();
        }

        public static async Task<IEnumerable<string>> GetVideoIDs(string playlist, int number = 30) {
            if (string.IsNullOrWhiteSpace(NadekoBot.Creds.GoogleAPIKey)) {
                throw new ArgumentNullException(nameof(playlist));
            }
            if (number < 1 || number > 100)
                throw new ArgumentOutOfRangeException();
            var link =
                $"https://www.googleapis.com/youtube/v3/playlistItems?part=contentDetails" +
                $"&maxResults={30}" +
                $"&playlistId={playlist}" +
                $"&key={NadekoBot.Creds.GoogleAPIKey}";

            var response = await GetResponseStringAsync(link);
            var obj = await Task.Run(() => JObject.Parse(response));

            return obj["items"].Select(item => "http://www.youtube.com/watch?v=" + item["contentDetails"]["videoId"]);
        }


        public static async Task<string> GetDanbooruImageLink(string tag) {
            var rng = new Random();

            if (tag == "loli") //loli doesn't work for some reason atm
                tag = "flat_chest";

            var link = $"http://danbooru.donmai.us/posts?" +
                        $"page={rng.Next(0, 15)}";
            if (!string.IsNullOrWhiteSpace(tag))
                link += $"&tags={tag.Replace(" ", "_")}";

            var webpage = await GetResponseStringAsync(link);
            var matches = Regex.Matches(webpage, "data-large-file-url=\"(?<id>.*?)\"");

            return $"http://danbooru.donmai.us" +
                   $"{matches[rng.Next(0, matches.Count)].Groups["id"].Value}";
        }

        public static async Task<string> GetGelbooruImageLink(string tag) {
            var rng = new Random();
            var url =
                $"http://gelbooru.com/index.php?page=post&s=list&pid={rng.Next(0, 10) * 42}&tags={tag.Replace(" ", "_")}";
            var webpage = await GetResponseStringAsync(url); // first extract the post id and go to that posts page
                                                             //src="htp://gelbooru.com/thumbnails/1b/5e/thumbnail_1b5e1dae36237ef0cd030575b93b5bd2.jpg?3064956"
            var matches = Regex.Matches(webpage, @"src=\""http:\/\/gelbooru\.com\/thumbnails\/" +
                                                 @"(?<folder>.*\/.*?)\/thumbnail_(?<id>.*?)\""");
            if (matches.Count == 0)
                throw new FileNotFoundException();
            var match = matches[rng.Next(0, matches.Count)];
            //http://simg4.gelbooru.com//images/58/20/58209047098e86c2f96c323fb85b8691.jpg?3076643
            return $"http://simg4.gelbooru.com//images/" +
                   $"{match.Groups["folder"]}/{match.Groups["id"]}";
        }

        internal static async Task<string> GetE621ImageLink(string tags) {
            var rng = new Random();
            var url = $"https://e621.net/post/index/{rng.Next(0, 5)}/{Uri.EscapeUriString(tags)}";
            var webpage = await GetResponseStringAsync(url); // first extract the post id and go to that posts page
            var matches = Regex.Matches(webpage, "\"file_url\":\"(?<url>.*?)\"");
            return matches[rng.Next(0, matches.Count)].Groups["url"].Value;
        }

        public static async Task<string> ShortenUrl(string url) {
            if (string.IsNullOrWhiteSpace(NadekoBot.Creds.GoogleAPIKey)) return url;
            try {
                var httpWebRequest =
                    (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/urlshortener/v1/url?key=" +
                                                       NadekoBot.Creds.GoogleAPIKey);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(await httpWebRequest.GetRequestStreamAsync())) {
                    var json = "{\"longUrl\":\"" + url + "\"}";
                    streamWriter.Write(json);
                }

                var httpResponse = (await httpWebRequest.GetResponseAsync()) as HttpWebResponse;
                if (httpResponse == null) return "HTTP_RESPONSE_ERROR";
                var responseStream = httpResponse.GetResponseStream();
                if (responseStream == null) return "RESPONSE_STREAM ERROR";
                using (var streamReader = new StreamReader(responseStream)) {
                    var responseText = await streamReader.ReadToEndAsync();
                    return Regex.Match(responseText, @"""id"": ?""(?<id>.+)""").Groups["id"].Value;
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return url;
            }
        }
    }
}