using System;
using Discord.Modules;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using NadekoBot.Classes;

namespace NadekoBot.Modules {
    internal class NSFW : DiscordModule {

        private readonly Random rng = new Random();

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.NSFW;

        public override void Install(ModuleManager manager) {
            manager.CreateCommands("", cgb => {

                cgb.AddCheck(Classes.Permissions.PermissionChecker.Instance);

                cgb.CreateCommand(Prefix + "hentai")
                    .Description("Shows a random NSFW hentai image from gelbooru and danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +)\n**Usage**: ~hentai yuri+kissing")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        var tag = e.GetArg("tag")?.Trim() ?? "";
                        await e.Channel.SendMessage(":heart: Gelbooru: " + await SearchHelper.GetGelbooruImageLink("rating%3Aexplicit+"+tag));
                        await e.Channel.SendMessage(":heart: Danbooru: " + await SearchHelper.GetDanbooruImageLink("rating%3Aexplicit+"+tag));
                    });
                cgb.CreateCommand(Prefix + "danbooru")
                    .Description("Shows a random hentai image from danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +)\n**Usage**: ~danbooru yuri+kissing")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        var tag = e.GetArg("tag")?.Trim() ?? "";
                        await e.Channel.SendMessage(await SearchHelper.GetDanbooruImageLink(tag));
                    });
                cgb.CreateCommand(Prefix + "gelbooru")
                    .Description("Shows a random hentai image from gelbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +)\n**Usage**: ~gelbooru yuri+kissing")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        var tag = e.GetArg("tag")?.Trim() ?? "";
                        await e.Channel.SendMessage(await SearchHelper.GetGelbooruImageLink(tag));
                    });
                cgb.CreateCommand(Prefix + "e621")
                    .Description("Shows a random hentai image from e621.net with a given tag. Tag is optional but preffered. Use spaces for multiple tags.\n**Usage**: ~e621 yuri+kissing")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e => {
                        var tag = e.GetArg("tag")?.Trim() ?? "";
                        await e.Channel.SendMessage(await SearchHelper.GetE621ImageLink(tag));
                    });
                cgb.CreateCommand(Prefix + "cp")
                    .Description("We all know where this will lead you to.")
                    .Parameter("anything", ParameterType.Unparsed)
                    .Do(async e => {
                        await e.Channel.SendMessage("http://i.imgur.com/MZkY1md.jpg");
                    });
                cgb.CreateCommand(Prefix + "boobs")
                    .Description("Real adult content.")
                    .Do(async e => {
                        try {
                            var obj = JArray.Parse(await SearchHelper.GetResponseStringAsync($"http://api.oboobs.ru/boobs/{rng.Next(0, 9380)}"))[0];
                            await e.Channel.SendMessage($"http://media.oboobs.ru/{ obj["preview"].ToString() }");
                        } catch (Exception ex) {
                            await e.Channel.SendMessage($"💢 {ex.Message}");
                        }
                    });
                cgb.CreateCommand(Prefix + "butts")
                    .Alias(Prefix + "ass",Prefix + "butt")
                    .Description("Real adult content.")
                    .Do(async e => {
                        try {
                            var obj = JArray.Parse(await SearchHelper.GetResponseStringAsync($"http://api.obutts.ru/butts/{rng.Next(0, 3373)}"))[0];
                            await e.Channel.SendMessage($"http://media.obutts.ru/{ obj["preview"].ToString() }");
                        } catch (Exception ex) {
                            await e.Channel.SendMessage($"💢 {ex.Message}");
                        }
                    });
            });
        }
    }
}
