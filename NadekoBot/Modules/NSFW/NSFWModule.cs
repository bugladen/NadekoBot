using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.Modules.Permissions.Classes;
using Newtonsoft.Json.Linq;
using System;

namespace NadekoBot.Modules.NSFW
{
    internal class NSFWModule : DiscordModule
    {

        private readonly Random rng = new Random();

        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.NSFW;

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {

                cgb.AddCheck(PermissionChecker.Instance);

                cgb.CreateCommand(Prefix + "hentai")
                    .Description("Shows a random NSFW hentai image from gelbooru and danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) | ~hentai yuri+kissing")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var tag = e.GetArg("tag")?.Trim() ?? "";
                        var gel = await SearchHelper.GetGelbooruImageLink("rating%3Aexplicit+" + tag).ConfigureAwait(false);
                        if (gel != null)
                            await e.Channel.SendMessage(":heart: Gelbooru: " + gel)
                                           .ConfigureAwait(false);
                        var dan = await SearchHelper.GetDanbooruImageLink("rating%3Aexplicit+" + tag).ConfigureAwait(false);
                        if (dan != null)
                            await e.Channel.SendMessage(":heart: Danbooru: " + dan)
                                           .ConfigureAwait(false);
                        if (dan == null && gel == null)
                            await e.Channel.SendMessage("`No results.`");
                    });
                cgb.CreateCommand(Prefix + "danbooru")
                    .Description("Shows a random hentai image from danbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) | ~danbooru yuri+kissing")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var tag = e.GetArg("tag")?.Trim() ?? "";
                        var link = await SearchHelper.GetDanbooruImageLink(tag).ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(link))
                            await e.Channel.SendMessage("Search yielded no results ;(");
                        else
                            await e.Channel.SendMessage(link).ConfigureAwait(false);
                    });
                cgb.CreateCommand(Prefix + "gelbooru")
                    .Description("Shows a random hentai image from gelbooru with a given tag. Tag is optional but preffered. (multiple tags are appended with +) | ~gelbooru yuri+kissing")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var tag = e.GetArg("tag")?.Trim() ?? "";
                        var link = await SearchHelper.GetGelbooruImageLink(tag).ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(link))
                            await e.Channel.SendMessage("Search yielded no results ;(");
                        else
                            await e.Channel.SendMessage(link).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "rule34")
                    .Description("Shows a random image from rule34.xx with a given tag. Tag is optional but preffered. (multiple tags are appended with +) | ~rule34 yuri+kissing")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var tag = e.GetArg("tag")?.Trim() ?? "";
                        var link = await SearchHelper.GetRule34ImageLink(tag).ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(link))
                            await e.Channel.SendMessage("Search yielded no results ;(");
                        else
                            await e.Channel.SendMessage(link).ConfigureAwait(false);
                    });
                cgb.CreateCommand(Prefix + "e621")
                    .Description("Shows a random hentai image from e621.net with a given tag. Tag is optional but preffered. Use spaces for multiple tags. | ~e621 yuri kissing")
                    .Parameter("tag", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var tag = e.GetArg("tag")?.Trim() ?? "";
                        await e.Channel.SendMessage(await SearchHelper.GetE621ImageLink(tag).ConfigureAwait(false)).ConfigureAwait(false);
                    });
                cgb.CreateCommand(Prefix + "cp")
                    .Description("We all know where this will lead you to.")
                    .Parameter("anything", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage("http://i.imgur.com/MZkY1md.jpg").ConfigureAwait(false);
                    });
                cgb.CreateCommand(Prefix + "boobs")
                    .Description("Real adult content.")
                    .Do(async e =>
                    {
                        try
                        {
                            var obj = JArray.Parse(await SearchHelper.GetResponseStringAsync($"http://api.oboobs.ru/boobs/{rng.Next(0, 9380)}").ConfigureAwait(false))[0];
                            await e.Channel.SendMessage($"http://media.oboobs.ru/{ obj["preview"].ToString() }").ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage($"💢 {ex.Message}").ConfigureAwait(false);
                        }
                    });
                cgb.CreateCommand(Prefix + "butts")
                    .Alias(Prefix + "ass", Prefix + "butt")
                    .Description("Real adult content.")
                    .Do(async e =>
                    {
                        try
                        {
                            var obj = JArray.Parse(await SearchHelper.GetResponseStringAsync($"http://api.obutts.ru/butts/{rng.Next(0, 3373)}").ConfigureAwait(false))[0];
                            await e.Channel.SendMessage($"http://media.obutts.ru/{ obj["preview"].ToString() }").ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage($"💢 {ex.Message}").ConfigureAwait(false);
                        }
                    });
            });
        }
    }
}
