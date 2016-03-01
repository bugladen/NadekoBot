using NadekoBot.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NadekoBot.Classes.JSONModels;

namespace NadekoBot.Classes {
    public enum RequestHttpMethod {
        Get, Post
    }

    public static class SearchHelper {

        public static async Task<Stream> GetResponseStream(string query, RequestHttpMethod method = RequestHttpMethod.Get) {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));
            var wr = (HttpWebRequest)WebRequest.Create(query);
            using (var response = await wr.GetResponseAsync()) {
                var stream = response?.GetResponseStream();
                if (stream == null)
                    throw new InvalidOperationException("Did not receive a response.");
                return stream;
            }
        }

        public static async Task<string> GetResponseAsync(string url, RequestHttpMethod method = RequestHttpMethod.Get, params Tuple<string,string>[] headers) {
            using (var httpClient = new HttpClient()) {
                if (headers != null) {
                    foreach (var header in headers) {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                return await httpClient.GetStringAsync(url);
            }
        }

        private static string token = "";
        public static async Task<AnimeResult> GetAnimeQueryResultLink(string query) {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            RefreshAnilistToken();

            var link = "http://anilist.co/api/anime/search/" + Uri.EscapeUriString(query);

            Dictionary<string, string> headers = new {"access_token" = token};
            var smallContent = await GetResponseAsync(link, headers);
            var smallObj = JArray.Parse(await httpClient.GetStringAsync(link))[0];
            var content = await httpClient.GetStringAsync("anime/" + smallObj["id"]);

            return await Task.Run(() => JsonConvert.DeserializeObject<AnimeResult>(content));
        }
        //todo kick out RestSharp and make it truly async
        public static async Task<MangaResult> GetMangaQueryResultLink(string query) {
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

        private static void RefreshAnilistToken() {
            try {
                var cl = new RestSharp.RestClient("http://anilist.co/api");
                var rq = new RestSharp.RestRequest("/auth/access_token", RestSharp.Method.POST);
                rq.AddParameter("grant_type", "client_credentials");
                rq.AddParameter("client_id", "kwoth-w0ki9");
                rq.AddParameter("client_secret", "Qd6j4FIAi1ZK6Pc7N7V4Z");
                var exec = cl.Execute(rq);

                token = JObject.Parse(exec.Content)["access_token"].ToString();
            } catch (Exception ex) {
                Console.WriteLine($"Failed refreshing anilist token:\n {ex}");
            }
        }

        public static async Task<bool> ValidateQuery(Discord.Channel ch, string query) {
            if (!string.IsNullOrEmpty(query.Trim())) return true;
            await ch.Send("Please specify search parameters.");
            return false;
        }

        public static async Task<string> FindYoutubeUrlByKeywords(string keywords) {
            if (string.IsNullOrWhiteSpace(NadekoBot.Creds.GoogleAPIKey))
                throw new InvalidCredentialException("Google API Key is missing.");
            if (keywords.Length > 150)
                throw new ArgumentException("Query is too long.");

            //maybe it is already a youtube url, in which case we will just extract the id and prepend it with youtube.com?v=
            var match = new Regex("(?:youtu\\.be\\/|v=)(?<id>[\\da-zA-Z\\-_]*)").Match(keywords);
            if (match.Length > 1) {
                return $"http://www.youtube.com?v={ match.Groups["id"].Value }";
            }
            var wr =
                WebRequest.Create(
                    $"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&q={Uri.EscapeDataString(keywords)}&key={NadekoBot.Creds.GoogleAPIKey}");
            try {
                using (var response = await wr.GetResponseAsync())
                using (var stream = response.GetResponseStream()) {
                    try {
                        using (var sr = new StreamReader(stream)) {
                            dynamic obj = JObject.Parse(await sr.ReadToEndAsync());
                            return "http://www.youtube.com/watch?v=" + obj.items[0].id.videoId.ToString();
                        }
                    } catch (Exception ex) {
                        ex.Message
                      }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error in findyoutubeurl: {ex.Message}");
                return string.Empty;
            }
        }

        public static async Task<string> GetPlaylistIdByKeyword(string v) {
            if (string.IsNullOrWhiteSpace(NadekoBot.Creds.GoogleAPIKey)) {
                Console.WriteLine("ERROR: No google api key found. Playing `Never gonna give you up`.");
                return string.Empty;
            }
            try {
                WebRequest wr = WebRequest.Create($"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&q={Uri.EscapeDataString(v)}&type=playlist&key={NadekoBot.Creds.GoogleAPIKey}");

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
            if (string.IsNullOrWhiteSpace(NadekoBot.Creds.GoogleAPIKey)) {
                Console.WriteLine("ERROR: No google api key found. Playing `Never gonna give you up`.");
                return toReturn;
            }
            try {
                WebRequest wr = WebRequest.Create($"https://www.googleapis.com/youtube/v3/playlistItems?part=contentDetails&maxResults={30}&playlistId={v}&key={ NadekoBot.Creds.GoogleAPIKey }");
                var response = await wr.GetResponseAsync();
                if (response == null) return toReturn;
                var responseStream = response.GetResponseStream();
                if (responseStream == null) return toReturn;
                var sr = new StreamReader(responseStream);

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


        public static async Task<string> GetDanbooruImageLink(string tag) {
            try {
                var rng = new Random();

                if (tag == "loli") //loli doesn't work for some reason atm
                    tag = "flat_chest";

                var webpage = await GetResponseAsync($"http://danbooru.donmai.us/posts?page={ rng.Next(0, 15) }&tags={ tag.Replace(" ", "_") }");
                var matches = Regex.Matches(webpage, "data-large-file-url=\"(?<id>.*?)\"");

                return await $"http://danbooru.donmai.us{ matches[rng.Next(0, matches.Count)].Groups["id"].Value }".ShortenUrl();
            } catch {
                return null;
            }
        }

        public static async Task<string> GetGelbooruImageLink(string tag) {
            try {
                var rng = new Random();
                var url = $"http://gelbooru.com/index.php?page=post&s=list&pid={ rng.Next(0, 10) * 42 }&tags={ tag.Replace(" ", "_") }";
                var webpage = await GetResponseAsync(url); // first extract the post id and go to that posts page
                var matches = Regex.Matches(webpage, "span id=\"s(?<id>\\d*)\"");
                var postLink = $"http://gelbooru.com/index.php?page=post&s=view&id={ matches[rng.Next(0, matches.Count)].Groups["id"].Value }";
                webpage = await GetResponseAsync(postLink);
                //now extract the image from post page
                var match = Regex.Match(webpage, "\"(?<url>http://simg4.gelbooru.com//images.*?)\"");
                return match.Groups["url"].Value;
            } catch {
                return null;
            }
        }

        internal static async Task<string> GetE621ImageLink(string tags) {
            try {
                var rng = new Random();
                var url = $"https://e621.net/post/index/{rng.Next(0, 5)}/{Uri.EscapeUriString(tags)}";
                var webpage = await GetResponseAsync(url); // first extract the post id and go to that posts page
                var matches = Regex.Matches(webpage, "\"file_url\":\"(?<url>.*?)\"");
                return matches[rng.Next(0, matches.Count)].Groups["url"].Value;
            } catch {
                return null;
            }
        }

        public static async Task<string> ShortenUrl(string url) {
            if (string.IsNullOrWhiteSpace(NadekoBot.Creds.GoogleAPIKey)) return url;
            try {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/urlshortener/v1/url?key=" + NadekoBot.Creds.GoogleAPIKey);
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
            } catch (Exception ex) { Console.WriteLine(ex.ToString()); return url; }
        }
    }
}
