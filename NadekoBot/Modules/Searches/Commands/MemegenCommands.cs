using Discord.Commands;
using NadekoBot.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NadekoBot.Modules.Searches.Commands
{
    class MemegenCommands : DiscordCommand
    {
        public MemegenCommands(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Prefix + "memelist")
                .Description($"Pulls a list of memes you can use with `~memegen` from http://memegen.link/templates/ | `{Prefix}memelist`")
                .Do(async e =>
                {
                    int i = 0;
                    await e.Channel.SendMessage("`List Of Commands:`\n```xl\n" +
                                string.Join("\n", JsonConvert.DeserializeObject<Dictionary<string, string>>(await SearchHelper.GetResponseStringAsync("http://memegen.link/templates/"))
                                      .Select(kvp => Path.GetFileName(kvp.Value))
                                      .GroupBy(item => (i++) / 4)
                                      .Select(ig => string.Concat(ig.Select(el => $"{el,-17}"))))
                                      + $"\n```").ConfigureAwait(false);
                });

            cgb.CreateCommand(Prefix + "memegen")
                .Description($"Generates a meme from memelist with top and bottom text. | `{Prefix}memegen biw \"gets iced coffee\" \"in the winter\"`")
                .Parameter("meme", ParameterType.Required)
                .Parameter("toptext", ParameterType.Required)
                .Parameter("bottext", ParameterType.Required)
                .Do(async e =>
                {
                    var meme = e.GetArg("meme");
                    var top = Uri.EscapeDataString(e.GetArg("toptext").Replace(' ', '-'));
                    var bot = Uri.EscapeDataString(e.GetArg("bottext").Replace(' ', '-'));
                    await e.Channel.SendMessage($"http://memegen.link/{meme}/{top}/{bot}.jpg");
                });
        }
    }
}
