//using Discord;
//using Discord.Commands;
//using NadekoBot.Attributes;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//// todo RestSharp
//namespace NadekoBot.Modules.Searches.Commands
//{
//    public partial class SearchesModule
//    {
//        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
//        [RequireContext(ContextType.Guild)]
//        public async Task Anime(IMessage imsg, [Remainder] string query = null)
//        {
//            var channel = imsg.Channel as ITextChannel;

//            if (!(await ValidateQuery(imsg.Channel as ITextChannel, query).ConfigureAwait(false))) return;
//            string result;
//            try
//            {
//                result = (await GetAnimeData(query).ConfigureAwait(false)).ToString();
//            }
//            catch
//            {
//                await imsg.Channel.SendMessageAsync("Failed to find that anime.").ConfigureAwait(false);
//                return;
//            }

//            await imsg.Channel.SendMessageAsync(result.ToString()).ConfigureAwait(false);
//        }

//        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
//        [RequireContext(ContextType.Guild)]
//        public async Task Manga(IMessage imsg, [Remainder] string query = null)
//        {
//            var channel = imsg.Channel as ITextChannel;

//            if (!(await ValidateQuery(imsg.Channel as ITextChannel, query).ConfigureAwait(false))) return;
//            string result;
//            try
//            {
//                result = (await GetMangaData(query).ConfigureAwait(false)).ToString();
//            }
//            catch
//            {
//                await imsg.Channel.SendMessageAsync("Failed to find that manga.").ConfigureAwait(false);
//                return;
//            }
//            await imsg.Channel.SendMessageAsync(result).ConfigureAwait(false);
//        }

//        public static async Task<AnimeResult> GetAnimeData(string query)
//        {
//            if (string.IsNullOrWhiteSpace(query))
//                throw new ArgumentNullException(nameof(query));

//            await RefreshAnilistToken().ConfigureAwait(false);

//            var link = "http://anilist.co/api/anime/search/" + Uri.EscapeUriString(query);
//            var smallContent = "";
//            var cl = new RestSharp.RestClient("http://anilist.co/api");
//            var rq = new RestSharp.RestRequest("/anime/search/" + Uri.EscapeUriString(query));
//            rq.AddParameter("access_token", token);
//            smallContent = cl.Execute(rq).Content;
//            var smallObj = JArray.Parse(smallContent)[0];

//            rq = new RestSharp.RestRequest("/anime/" + smallObj["id"]);
//            rq.AddParameter("access_token", token);
//            var content = cl.Execute(rq).Content;

//            return await Task.Run(() => JsonConvert.DeserializeObject<AnimeResult>(content)).ConfigureAwait(false);
//        }

//        public static async Task<MangaResult> GetMangaData(string query)
//        {
//            if (string.IsNullOrWhiteSpace(query))
//                throw new ArgumentNullException(nameof(query));

//            await RefreshAnilistToken().ConfigureAwait(false);

//            var link = "http://anilist.co/api/anime/search/" + Uri.EscapeUriString(query);
//            var smallContent = "";
//            var cl = new RestSharp.RestClient("http://anilist.co/api");
//            var rq = new RestSharp.RestRequest("/manga/search/" + Uri.EscapeUriString(query));
//            rq.AddParameter("access_token", token);
//            smallContent = cl.Execute(rq).Content;
//            var smallObj = JArray.Parse(smallContent)[0];

//            rq = new RestSharp.RestRequest("/manga/" + smallObj["id"]);
//            rq.AddParameter("access_token", token);
//            var content = cl.Execute(rq).Content;

//            return await Task.Run(() => JsonConvert.DeserializeObject<MangaResult>(content)).ConfigureAwait(false);
//        }
//    }
//}
