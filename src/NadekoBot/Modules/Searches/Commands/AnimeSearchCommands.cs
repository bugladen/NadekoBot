using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class AnimeSearchCommands
        {
            private Logger _log;

            private string anilistToken { get; set; }
            private DateTime lastRefresh { get; set; }

            public AnimeSearchCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Anime(IUserMessage umsg, [Remainder] string query)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (string.IsNullOrWhiteSpace(query))
                    return;

                var result = await GetAnimeData(query).ConfigureAwait(false);

                await channel.SendMessageAsync(result.ToString() ?? "`No anime found.`").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Manga(IUserMessage umsg, [Remainder] string query)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (string.IsNullOrWhiteSpace(query))
                    return;

                var result = await GetMangaData(query).ConfigureAwait(false);

                await channel.SendMessageAsync(result.ToString() ?? "`No manga found.`").ConfigureAwait(false);
            }

            private async Task<AnimeResult> GetAnimeData(string query)
            {
                if (string.IsNullOrWhiteSpace(query))
                    throw new ArgumentNullException(nameof(query));
                try
                {
                    await RefreshAnilistToken().ConfigureAwait(false);

                    var link = "http://anilist.co/api/anime/search/" + Uri.EscapeUriString(query);
                    using (var http = new HttpClient())
                    {
                        var res = await http.GetStringAsync("http://anilist.co/api/anime/search/" + Uri.EscapeUriString(query) + $"?access_token={anilistToken}").ConfigureAwait(false);
                        var smallObj = JArray.Parse(res)[0];
                        var aniData = await http.GetStringAsync("http://anilist.co/api/anime/" + smallObj["id"] + $"?access_token={anilistToken}").ConfigureAwait(false);

                        return await Task.Run(() => { try { return JsonConvert.DeserializeObject<AnimeResult>(aniData); } catch { return null; } }).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) {
                    _log.Warn(ex, "Failed anime search for {0}", query);
                    return null;
                }
            }

            private async Task RefreshAnilistToken()
            {
                if (DateTime.Now - lastRefresh > TimeSpan.FromMinutes(29))
                    lastRefresh = DateTime.Now;
                else
                {
                    return;
                }
                var headers = new Dictionary<string, string> {
                    {"grant_type", "client_credentials"},
                    {"client_id", "kwoth-w0ki9"},
                    {"client_secret", "Qd6j4FIAi1ZK6Pc7N7V4Z"},
                };
                using (var http = new HttpClient())
                {
                    http.AddFakeHeaders();
                    var formContent = new FormUrlEncodedContent(headers);
                    var response = await http.PostAsync("http://anilist.co/api/auth/access_token", formContent).ConfigureAwait(false);
                    var stringContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    anilistToken = JObject.Parse(stringContent)["access_token"].ToString();
                }
                
            }

            private async Task<MangaResult> GetMangaData(string query)
            {
                if (string.IsNullOrWhiteSpace(query))
                    throw new ArgumentNullException(nameof(query));
                try
                {
                    await RefreshAnilistToken().ConfigureAwait(false);
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
}