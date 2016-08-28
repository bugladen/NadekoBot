using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Modules.Searches.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class JokeCommands
        {
            //todo DB
            private List<WoWJoke> wowJokes = new List<WoWJoke>();
            private List<MagicItem> magicItems;
            private Logger _log;

            public JokeCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
                if (File.Exists("data/wowjokes.json"))
                {
                    wowJokes = JsonConvert.DeserializeObject<List<WoWJoke>>(File.ReadAllText("data/wowjokes.json"));
                }
                else
                    _log.Warn("data/wowjokes.json is missing. WOW Jokes are not loaded.");

                if (File.Exists("data/magicitems.json"))
                {
                    magicItems = JsonConvert.DeserializeObject<List<MagicItem>>(File.ReadAllText("data/magicitems.json"));
                }
                else
                    _log.Warn("data/magicitems.json is missing. Magic items are not loaded.");
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task Yomama(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://api.yomomma.info/").ConfigureAwait(false);
                    await channel.SendMessageAsync("`" + JObject.Parse(response)["joke"].ToString() + "` 😆").ConfigureAwait(false);
                }
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task Randjoke(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://tambal.azurewebsites.net/joke/random").ConfigureAwait(false);
                    await channel.SendMessageAsync("`" + JObject.Parse(response)["joke"].ToString() + "` 😆").ConfigureAwait(false);
                }
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task ChuckNorris(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://api.icndb.com/jokes/random/").ConfigureAwait(false);
                    await channel.SendMessageAsync("`" + JObject.Parse(response)["value"]["joke"].ToString() + "` 😆").ConfigureAwait(false);
                }
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task WowJoke(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (!wowJokes.Any())
                {
                }
                await channel.SendMessageAsync(wowJokes[new Random().Next(0, wowJokes.Count)].ToString());
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task MagicItem(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;
                var rng = new Random();
                var item = magicItems[rng.Next(0, magicItems.Count)].ToString();

                await channel.SendMessageAsync(item).ConfigureAwait(false);
            }
        }
    }
}
