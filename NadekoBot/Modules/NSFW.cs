using System;
using Discord.Modules;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using NadekoBot.Classes;

namespace NadekoBot.Modules {
    internal class NSFW : DiscordModule {

        private Random _r = new Random();

        public NSFW()  {

        }

        public override void Install(ModuleManager manager) {
            manager.CreateCommands("", cgb => {

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                cgb.CreateCommand("~hentai")
                    .Description("Shows a random NSFW hentai image from gelbooru and danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +)\n**Usage**: ~hentai yuri+kissing")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        string tag = e.GetArg("tag");
                        if (tag == null)
                            tag = "";
                        await e.Channel.SendMessage(":heart: Gelbooru: " + await SearchHelper.GetGelbooruImageLink(tag));
                        await e.Channel.SendMessage(":heart: Danbooru: " + await SearchHelper.GetDanbooruImageLink(tag));
                    });
                cgb.CreateCommand("~danbooru")
                    .Description("Shows a random hentai image from danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +)\n**Usage**: ~danbooru yuri+kissing")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        string tag = e.GetArg("tag");
                        if (tag == null)
                            tag = "";
                        await e.Channel.SendMessage(await SearchHelper.GetDanbooruImageLink(tag));
                    });
                cgb.CreateCommand("~gelbooru")
                    .Description("Shows a random hentai image from gelbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +)\n**Usage**: ~gelbooru yuri+kissing")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        string tag = e.GetArg("tag");
                        if (tag == null)
                            tag = "";
                        await e.Channel.SendMessage(await SearchHelper.GetGelbooruImageLink(tag));
                    });
                cgb.CreateCommand("~e621")
                    .Description("Shows a random hentai image from e621.net with a given tag. Tag is optional but preffered. Use spaces for multiple tags.\n**Usage**: ~e621 yuri+kissing")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        string tag = e.GetArg("tag");
                        if (tag == null)
                            tag = "";
                        await e.Channel.SendMessage(await SearchHelper.GetE621ImageLink(tag));
                    });
                cgb.CreateCommand("~cp")
                    .Description("We all know where this will lead you to.")
                    .Parameter("anything", ParameterType.Unparsed)
                    .Do(async e => {
                        await e.Channel.SendMessage("http://i.imgur.com/MZkY1md.jpg");
                    });
                cgb.CreateCommand("~boobs")
                    .Description("Real adult content.")
                    .Do(async e => {
                        try {
                            var obj = JArray.Parse(await SearchHelper.GetResponseAsync($"http://api.oboobs.ru/boobs/{_r.Next(0, 9304)}"))[0];
                            await e.Channel.SendMessage($"http://media.oboobs.ru/{ obj["preview"].ToString() }");
                        } catch (Exception ex) {
                            await e.Channel.SendMessage($"💢 {ex.Message}");
                        }
                    });
            });
        }
    }
}
