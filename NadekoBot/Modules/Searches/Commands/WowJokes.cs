using NadekoBot.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Commands;
using Newtonsoft.Json;

namespace NadekoBot.Modules.Searches.Commands
{
    class WowJokes : DiscordCommand
    {
        public WowJokes(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            List<WowJokes> Jokes = new List<WowJokes>();

            cgb.CreateCommand(Module.Prefix + "wowjoke")
                .Description("Get one of Kwoth's penultimate WoW jokes.")
                .Do(async e =>
                {
                    if (!Jokes.Any())
                    {                       
                        Jokes = JsonConvert.DeserializeObject<List<WowJokes>>("data/wowjokes.json");
                    }
                    await e.Channel.SendMessage(Jokes[new Random().Next(0, Jokes.Count)].ToString());
                });
        }
    }
}
