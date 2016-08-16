using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Modules.Searches.Commands.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches.Commands
{
    public partial class SearchesModule
    {
        [Group]
        public class JokeCommands
        {
            //todo DB
            private List<WoWJoke> wowJokes;
            private List<MagicItem> magicItems;

            public JokeCommands()
            {
                wowJokes = JsonConvert.DeserializeObject<List<WoWJoke>>(File.ReadAllText("data/wowjokes.json"));
                magicItems = JsonConvert.DeserializeObject<List<MagicItem>>(File.ReadAllText("data/magicitems.json"));
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task Yomama(IMessage imsg)
            {
                var channel = imsg.Channel as IGuildChannel;
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://api.yomomma.info/").ConfigureAwait(false);
                    await imsg.Channel.SendMessageAsync("`" + JObject.Parse(response)["joke"].ToString() + "` 😆").ConfigureAwait(false);
                }
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task Randjoke(IMessage imsg)
            {
                var channel = imsg.Channel as IGuildChannel;
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://tambal.azurewebsites.net/joke/random").ConfigureAwait(false);
                    await imsg.Channel.SendMessageAsync("`" + JObject.Parse(response)["joke"].ToString() + "` 😆").ConfigureAwait(false);
                }
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task ChuckNorris(IMessage imsg)
            {
                var channel = imsg.Channel as IGuildChannel;
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://tambal.azurewebsites.net/joke/random").ConfigureAwait(false);
                    await imsg.Channel.SendMessageAsync("`" + JObject.Parse(response)["joke"].ToString() + "` 😆").ConfigureAwait(false);
                }
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task WowJoke(IMessage imsg)
            {
                var channel = imsg.Channel as IGuildChannel;

                if (!wowJokes.Any())
                {
                }
                await imsg.Channel.SendMessageAsync(wowJokes[new Random().Next(0, wowJokes.Count)].ToString());
            }

            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task MagicItem(IMessage imsg)
            {
                var channel = imsg.Channel as IGuildChannel;
                var rng = new Random();
                var item = magicItems[rng.Next(0, magicItems.Count)].ToString();

                await imsg.Channel.SendMessageAsync(item).ConfigureAwait(false);
            }
        }
    }
}
