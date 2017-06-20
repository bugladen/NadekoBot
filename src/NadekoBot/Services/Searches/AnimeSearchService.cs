using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Services.Searches
{
    //todo move to the website
    public class AnimeSearchService
    {
        private readonly Timer _anilistTokenRefresher;
        private readonly Logger _log;

        private static string anilistToken { get; set; }

        public AnimeSearchService()
        {
            _log = LogManager.GetCurrentClassLogger();
            _anilistTokenRefresher = new Timer(async (state) =>
            {
                try
                {
                    var headers = new Dictionary<string, string>
                        {
                            {"grant_type", "client_credentials"},
                            {"client_id", "kwoth-w0ki9"},
                            {"client_secret", "Qd6j4FIAi1ZK6Pc7N7V4Z"},
                        };

                    using (var http = new HttpClient())
                    {
                        //http.AddFakeHeaders();
                        http.DefaultRequestHeaders.Clear();
                        var formContent = new FormUrlEncodedContent(headers);
                        var response = await http.PostAsync("https://anilist.co/api/auth/access_token", formContent).ConfigureAwait(false);
                        var stringContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        anilistToken = JObject.Parse(stringContent)["access_token"].ToString();
                    }
                }
                catch
                {
                    // ignored
                }
            }, null, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(29));
        }

        public async Task<AnimeResult> GetAnimeData(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));
            try
            {

                var link = "http://anilist.co/api/anime/search/" + Uri.EscapeUriString(query);
                using (var http = new HttpClient())
                {
                    var res = await http.GetStringAsync(link + $"?access_token={anilistToken}").ConfigureAwait(false);
                    var smallObj = JArray.Parse(res)[0];
                    var aniData = await http.GetStringAsync("http://anilist.co/api/anime/" + smallObj["id"] + $"?access_token={anilistToken}").ConfigureAwait(false);

                    return await Task.Run(() => { try { return JsonConvert.DeserializeObject<AnimeResult>(aniData); } catch { return null; } }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex, "Failed anime search for {0}", query);
                return null;
            }
        }

        public async Task<MangaResult> GetMangaData(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));
            try
            {
                using (var http = new HttpClient())
                {
                    var res = await http.GetStringAsync("http://anilist.co/api/manga/search/" + Uri.EscapeUriString(query) + $"?access_token={anilistToken}").ConfigureAwait(false);
                    var smallObj = JArray.Parse(res)[0];
                    var aniData = await http.GetStringAsync("http://anilist.co/api/manga/" + smallObj["id"] + $"?access_token={anilistToken}").ConfigureAwait(false);

                    return await Task.Run(() => { try { return JsonConvert.DeserializeObject<MangaResult>(aniData); } catch { return null; } }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex, "Failed anime search for {0}", query);
                return null;
            }
        }
    }
}
