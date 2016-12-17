using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Attributes;
using System.Net.Http;
using NadekoBot.Extensions;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [NadekoCommand, Usage, Description, Aliases]
        public async Task Memelist()
        {
            HttpClientHandler handler = new HttpClientHandler();

            handler.AllowAutoRedirect = false;

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
            var top = Uri.EscapeDataString(topText.Replace(' ', '-'));
            var bot = Uri.EscapeDataString(botText.Replace(' ', '-'));
            await Context.Channel.SendMessageAsync($"http://memegen.link/{meme}/{top}/{bot}.jpg")
                         .ConfigureAwait(false);
        }
    }
}
