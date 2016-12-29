using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Models;
using NadekoBot.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System.Collections.Generic;
using System.Diagnostics;
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
            private static List<WoWJoke> wowJokes { get; } = new List<WoWJoke>();
            private static List<MagicItem> magicItems { get; } = new List<MagicItem>();
            private static Logger _log { get; }

            static JokeCommands()
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

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Yomama(IUserMessage msg)
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://api.yomomma.info/").ConfigureAwait(false);
                    System.Console.WriteLine(response);
                    await msg.Channel.SendConfirmAsync(JObject.Parse(response)["joke"].ToString() + " 😆").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Randjoke(IUserMessage msg)
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://tambal.azurewebsites.net/joke/random").ConfigureAwait(false);
                    await msg.Channel.SendConfirmAsync(JObject.Parse(response)["joke"].ToString() + " 😆").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ChuckNorris(IUserMessage msg)
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://api.icndb.com/jokes/random/").ConfigureAwait(false);
                    await msg.Channel.SendConfirmAsync(JObject.Parse(response)["value"]["joke"].ToString() + " 😆").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WowJoke(IUserMessage msg)
            {
                if (!wowJokes.Any())
                {
                    await msg.Channel.SendErrorAsync("Jokes not loaded.").ConfigureAwait(false);
                    return;
                }
                var joke = wowJokes[new NadekoRandom().Next(0, wowJokes.Count)];
                await msg.Channel.SendConfirmAsync(joke.Question, joke.Answer).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task MagicItem(IUserMessage msg)
            {
                if (!wowJokes.Any())
                {
                    await msg.Channel.SendErrorAsync("MagicItems not loaded.").ConfigureAwait(false);
                    return;
                }
                var item = magicItems[new NadekoRandom().Next(0, magicItems.Count)];

                await msg.Channel.SendConfirmAsync("✨" + item.Name, item.Description).ConfigureAwait(false);
            }
        }
    }
}
