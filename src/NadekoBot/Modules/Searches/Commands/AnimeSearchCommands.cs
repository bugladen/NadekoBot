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
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class AnimeSearchCommands : ModuleBase
        {
            private static Timer anilistTokenRefresher { get; }
            private static Logger _log { get; }
            private static string anilistToken { get; set; }

            static AnimeSearchCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
                anilistTokenRefresher = new Timer(async (state) =>
                {
                    try
                    {
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
                    catch (Exception ex)
                    {
                        _log.Error(ex);
                    }
                }, null, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(29));
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Anime([Remainder] string query)
            {
                if (string.IsNullOrWhiteSpace(query))
                    return;

                var animeData = await GetAnimeData(query).ConfigureAwait(false);

                if (animeData == null)
                {
                    await Context.Channel.SendErrorAsync("Failed finding that animu.").ConfigureAwait(false);
                    return;
                }

                var embed = new EmbedBuilder().WithColor(NadekoBot.OkColor)
                    .WithDescription(animeData.Synopsis.Replace("<br>", Environment.NewLine))
                    .WithTitle(animeData.title_english)
                    .WithUrl(animeData.Link)
                    .WithImageUrl(animeData.image_url_lge)
                    .AddField(efb => efb.WithName("Episodes").WithValue(animeData.total_episodes.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName("Status").WithValue(animeData.AiringStatus.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName("Genres").WithValue(String.Join(", ", animeData.Genres)).WithIsInline(true))
                    .WithFooter(efb => efb.WithText("Score: " + animeData.average_score + " / 100"));
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Manga([Remainder] string query)
            {
                if (string.IsNullOrWhiteSpace(query))
                    return;

                var mangaData = await GetMangaData(query).ConfigureAwait(false);

                if (mangaData == null)
                {
                    await Context.Channel.SendErrorAsync("Failed finding that mango.").ConfigureAwait(false);
                    return;
                }

                var embed = new EmbedBuilder().WithColor(NadekoBot.OkColor)
                    .WithDescription(mangaData.Synopsis.Replace("<br>", Environment.NewLine))
                    .WithTitle(mangaData.title_english)
                    .WithUrl(mangaData.Link)
                    .WithImageUrl(mangaData.image_url_lge)
                    .AddField(efb => efb.WithName("Episodes").WithValue(mangaData.total_chapters.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName("Status").WithValue(mangaData.publishing_status.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName("Genres").WithValue(String.Join(", ", mangaData.Genres)).WithIsInline(true))
                    .WithFooter(efb => efb.WithText("Score: " + mangaData.average_score + " / 100"));

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            private async Task<AnimeResult> GetAnimeData(string query)
            {
                if (string.IsNullOrWhiteSpace(query))
                    throw new ArgumentNullException(nameof(query));
                try
                {

                    var link = "http://anilist.co/api/anime/search/" + Uri.EscapeUriString(query);
                    using (var http = new HttpClient())
                    {
                        var res = await http.GetStringAsync("http://anilist.co/api/anime/search/" + Uri.EscapeUriString(query) + $"?access_token={anilistToken}").ConfigureAwait(false);
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

            private async Task<MangaResult> GetMangaData(string query)
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
}