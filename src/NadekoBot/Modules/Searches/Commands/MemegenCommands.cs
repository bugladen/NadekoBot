using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Discord;
using NadekoBot.Services;
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
            using (var http = new HttpClient())
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(await http.GetStringAsync("http://memegen.link/templates/"))
                                          .Select(kvp => Path.GetFileName(kvp.Value));

                await channel.SendTableAsync(data, x => $"{x,-17}", 3);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Memegen(IUserMessage umsg, string meme, string topText, string botText)
        {
            var channel = (ITextChannel)umsg.Channel;

            var top = Uri.EscapeDataString(topText.Replace(' ', '-'));
            var bot = Uri.EscapeDataString(botText.Replace(' ', '-'));
            await channel.SendMessageAsync($"http://memegen.link/{meme}/{top}/{bot}.jpg");
        }
    }
}
