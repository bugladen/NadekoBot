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
        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Memelist(IMessage imsg)
        {
            var channel = imsg.Channel as ITextChannel;
            using (var http = new HttpClient())
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(await http.GetStringAsync("http://memegen.link/templates/"))
                                          .Select(kvp => Path.GetFileName(kvp.Value));

                await channel.SendTableAsync(data, x => $"{x,-17}", 3);
            }
        }

        [LocalizedCommand, LocalizedDescription, LocalizedSummary]
        [RequireContext(ContextType.Guild)]
        public async Task Memegen(IMessage imsg, string meme, string topText, string botText)
        {
            var channel = imsg.Channel as ITextChannel;

            var top = Uri.EscapeDataString(topText.Replace(' ', '-'));
            var bot = Uri.EscapeDataString(botText.Replace(' ', '-'));
            await channel.SendMessageAsync($"http://memegen.link/{meme}/{top}/{bot}.jpg");
        }
    }
}
