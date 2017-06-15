using AngleSharp;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Searches;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class JokeCommands : NadekoSubmodule
        {
            private readonly SearchesService _searches;

            public JokeCommands(SearchesService searches)
            {
                _searches = searches;
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Yomama()
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://api.yomomma.info/").ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync(JObject.Parse(response)["joke"].ToString() + " 😆").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Randjoke()
            {
                using (var http = new HttpClient())
                {
                    http.AddFakeHeaders();

                    var config = Configuration.Default.WithDefaultLoader();
                    var document = await BrowsingContext.New(config).OpenAsync("http://www.goodbadjokes.com/random");

                    var html = document.QuerySelector(".post > .joke-content");

                    var part1 = html.QuerySelector("dt").TextContent;
                    var part2 = html.QuerySelector("dd").TextContent;

                    await Context.Channel.SendConfirmAsync("", part1 + "\n\n" + part2, footer: document.BaseUri).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task ChuckNorris()
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://api.icndb.com/jokes/random/").ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync(JObject.Parse(response)["value"]["joke"].ToString() + " 😆").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task WowJoke()
            {
                if (!_searches.WowJokes.Any())
                {
                    await ReplyErrorLocalized("jokes_not_loaded").ConfigureAwait(false);
                    return;
                }
                var joke = _searches.WowJokes[new NadekoRandom().Next(0, _searches.WowJokes.Count)];
                await Context.Channel.SendConfirmAsync(joke.Question, joke.Answer).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task MagicItem()
            {
                if (!_searches.WowJokes.Any())
                {
                    await ReplyErrorLocalized("magicitems_not_loaded").ConfigureAwait(false);
                    return;
                }
                var item = _searches.MagicItems[new NadekoRandom().Next(0, _searches.MagicItems.Count)];

                await Context.Channel.SendConfirmAsync("✨" + item.Name, item.Description).ConfigureAwait(false);
            }
        }
    }
}
