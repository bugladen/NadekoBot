using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class MemegenCommands : NadekoSubmodule
        {
            private static readonly ImmutableDictionary<char, string> _map = new Dictionary<char, string>()
            {
                {'?', "~q"},
                {'%', "~p"},
                {'#', "~h"},
                {'/', "~s"},
                {' ', "-"},
                {'-', "--"},
                {'_', "__"},
                {'"', "''"}

            }.ToImmutableDictionary();

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Memelist()
            {
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false
                };


                using (var http = new HttpClient(handler))
                {
                    var rawJson = await http.GetStringAsync("https://memegen.link/api/templates/").ConfigureAwait(false);
                    var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(rawJson)
                        .Select(kvp => Path.GetFileName(kvp.Value));

                    await Context.Channel.SendTableAsync(data, x => $"{x,-17}", 3).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Memegen(string meme, string topText, string botText)
            {
                var top = Replace(topText);
                var bot = Replace(botText);
                await Context.Channel.SendMessageAsync($"http://memegen.link/{meme}/{top}/{bot}.jpg")
                    .ConfigureAwait(false);
            }

            private static string Replace(string input)
            {
                var sb = new StringBuilder();

                foreach (var c in input)
                {
                    if (_map.TryGetValue(c, out var tmp))
                        sb.Append(tmp);
                    else
                        sb.Append(c);
                }

                return sb.ToString();
            }
        }
    }
}