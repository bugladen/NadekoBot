using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Discord;
using System.Threading.Tasks;
using NadekoBot.Attributes;
using System.Net.Http;
using NadekoBot.Extensions;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Memelist(IUserMessage umsg)
        {
            var channel = (ITextChannel)umsg.Channel;
            HttpClientHandler handler = new HttpClientHandler();

            handler.AllowAutoRedirect = false;

            using (var http = new HttpClient(handler))
            {
                var rawJson = await http.GetStringAsync("https://memegen.link/api/templates/").ConfigureAwait(false);
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(rawJson)
                                      .Select(kvp => Path.GetFileName(kvp.Value));

                await channel.SendTableAsync(data, x => $"{x,-17}", 3).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Memegen(IUserMessage umsg, string meme, string topText, string botText)
        {
            var channel = (ITextChannel)umsg.Channel;

            var top = topText.Replace(' ', '-');
            var bot = botText.Replace(' ', '-');
            await channel.SendMessageAsync($"http://memegen.link/{meme}/{top}/{bot}.jpg")
                         .ConfigureAwait(false);
        }
    }
}
