using System;
using Discord.Modules;
using NadekoBot.Extensions;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using NadekoBot.Classes;

namespace NadekoBot.Modules {
    class NSFW : DiscordModule {

        private Random _r = new Random();

        public NSFW() : base() {

        }

        public override void Install(ModuleManager manager) {
            manager.CreateCommands("", cgb => {
                cgb.CreateCommand("~hentai")
                    .Description("Shows a random NSFW hentai image from gelbooru and danbooru with a given tag. Tag is optional but preffered.\n**Usage**: ~hentai yuri")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        string tag = e.GetArg("tag");
                        if (tag == null)
                            tag = "";
                        await e.Send(":heart: Gelbooru: " + await SearchHelper.GetGelbooruImageLink(tag));
                        await e.Send(":heart: Danbooru: " + await SearchHelper.GetDanbooruImageLink(tag));
                    });
                cgb.CreateCommand("~danbooru")
                    .Description("Shows a random hentai image from danbooru with a given tag. Tag is optional but preffered.\n**Usage**: ~hentai yuri")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        string tag = e.GetArg("tag");
                        if (tag == null)
                            tag = "";
                        await e.Send(await SearchHelper.GetDanbooruImageLink(tag));
                    });
                cgb.CreateCommand("~gelbooru")
                    .Description("Shows a random hentai image from gelbooru with a given tag. Tag is optional but preffered.\n**Usage**: ~hentai yuri")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        string tag = e.GetArg("tag");
                        if (tag == null)
                            tag = "";
                        await e.Send(await SearchHelper.GetGelbooruImageLink(tag));
                    });
                cgb.CreateCommand("~cp")
                    .Description("We all know where this will lead you to.")
                    .Parameter("anything", ParameterType.Unparsed)
                    .Do(async e => {
                        await e.Send("http://i.imgur.com/MZkY1md.jpg");
                    });
                cgb.CreateCommand("~boobs")
                    .Description("Real adult content.")
                    .Do(async e => {
                        try {
                            var obj = JArray.Parse(await SearchHelper.GetResponseAsync($"http://api.oboobs.ru/boobs/{_r.Next(0, 9304)}"))[0];
                            await e.Send($"http://media.oboobs.ru/{ obj["preview"].ToString() }");
                        } catch (Exception ex) {
                            await e.Send($"💢 {ex.Message}");
                        }
                    });
            });
        }
    }
}
