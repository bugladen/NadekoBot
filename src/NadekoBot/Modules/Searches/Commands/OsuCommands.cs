using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class OsuCommands
        {
            private static Logger _log { get; }

            static OsuCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
            }
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Osu(IUserMessage umsg, string usr, [Remainder] string mode = null)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (string.IsNullOrWhiteSpace(usr))
                    return;

                using (HttpClient http = new HttpClient())
                {
                    try
                    {
                        var m = 0;
                        if (!string.IsNullOrWhiteSpace(mode))
                        {
                            m = ResolveGameMode(mode);
                        }
                        http.AddFakeHeaders();
                        var res = await http.GetStreamAsync(new Uri($"http://lemmmy.pw/osusig/sig.php?uname={ usr }&flagshadow&xpbar&xpbarhex&pp=2&mode={m}")).ConfigureAwait(false);

                        MemoryStream ms = new MemoryStream();
                        res.CopyTo(ms);
                        ms.Position = 0;
                        await channel.SendFileAsync(ms, $"{usr}.png", $"🎧 **Profile Link: **https://osu.ppy.sh/u/{Uri.EscapeDataString(usr)}\n`Image provided by https://lemmmy.pw/osusig`").ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await channel.SendErrorAsync("Failed retrieving osu signature.").ConfigureAwait(false);
                        _log.Warn(ex, "Osu command failed");
                    }
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Osub(IUserMessage umsg, [Remainder] string map)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.OsuApiKey))
                {
                    await channel.SendErrorAsync("An osu! API key is required.").ConfigureAwait(false);
                    return;
                }

                if (string.IsNullOrWhiteSpace(map))
                    return;

                try
                {
                    using (var http = new HttpClient())
                    {
                        var mapId = ResolveMap(map);
                        var reqString = $"https://osu.ppy.sh/api/get_beatmaps?k={NadekoBot.Credentials.OsuApiKey}&{mapId}";
                        var obj = JArray.Parse(await http.GetStringAsync(reqString).ConfigureAwait(false))[0];
                        var sb = new System.Text.StringBuilder();
                        var starRating = Math.Round(Double.Parse($"{obj["difficultyrating"]}", CultureInfo.InvariantCulture), 2);
                        var time = TimeSpan.FromSeconds(Double.Parse($"{obj["total_length"]}")).ToString(@"mm\:ss");
                        sb.AppendLine($"{obj["artist"]} - {obj["title"]}, mapped by {obj["creator"]}. https://osu.ppy.sh/s/{obj["beatmapset_id"]}");
                        sb.AppendLine($"{starRating} stars, {obj["bpm"]} BPM | AR{obj["diff_approach"]}, CS{obj["diff_size"]}, OD{obj["diff_overall"]} | Length: {time}");
                        await channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    await channel.SendErrorAsync("Something went wrong.");
                    _log.Warn(ex, "Osub command failed");
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Osu5(IUserMessage umsg, string user, [Remainder] string mode = null)
            {
                var channel = (ITextChannel)umsg.Channel;
                if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.OsuApiKey))
                {
                    await channel.SendErrorAsync("An osu! API key is required.").ConfigureAwait(false);
                    return;
                }

                if (string.IsNullOrWhiteSpace(user))
                {
                    await channel.SendErrorAsync("Please provide a username.").ConfigureAwait(false);
                    return;
                }
                using (var http = new HttpClient())
                {
                    try
                    {
                        var m = 0;
                        if (!string.IsNullOrWhiteSpace(mode))
                        {
                            m = ResolveGameMode(mode);
                        }

                        var reqString = $"https://osu.ppy.sh/api/get_user_best?k={NadekoBot.Credentials.OsuApiKey}&u={Uri.EscapeDataString(user)}&type=string&limit=5&m={m}";
                        var obj = JArray.Parse(await http.GetStringAsync(reqString).ConfigureAwait(false));
                        var sb = new System.Text.StringBuilder($"`Top 5 plays for {user}:`\n```xl" + Environment.NewLine);
                        foreach (var item in obj)
                        {
                            var mapReqString = $"https://osu.ppy.sh/api/get_beatmaps?k={NadekoBot.Credentials.OsuApiKey}&b={item["beatmap_id"]}";
                            var map = JArray.Parse(await http.GetStringAsync(mapReqString).ConfigureAwait(false))[0];
                            var pp = Math.Round(Double.Parse($"{item["pp"]}", CultureInfo.InvariantCulture), 2);
                            var acc = CalculateAcc(item, m);
                            var mods = ResolveMods(Int32.Parse($"{item["enabled_mods"]}"));
                            if (mods != "+")
                                sb.AppendLine($"{pp + "pp",-7} | {acc + "%",-7} | {map["artist"] + "-" + map["title"] + " (" + map["version"] + ")",-40} | **{mods,-10}** | /b/{item["beatmap_id"]}");
                            else
                                sb.AppendLine($"{pp + "pp",-7} | {acc + "%",-7} | {map["artist"] + "-" + map["title"] + " (" + map["version"] + ")",-40}  | /b/{item["beatmap_id"]}");
                        }
                        sb.Append("```");
                        await channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await channel.SendErrorAsync("Something went wrong.");
                        _log.Warn(ex, "Osu5 command failed");
                    }

                }
            }

            //https://osu.ppy.sh/wiki/Accuracy
            private static Double CalculateAcc(JToken play, int mode)
            {
                if (mode == 0)
                {
                    var hitPoints = Double.Parse($"{play["count50"]}") * 50 + Double.Parse($"{play["count100"]}") * 100 + Double.Parse($"{play["count300"]}") * 300;
                    var totalHits = Double.Parse($"{play["count50"]}") + Double.Parse($"{play["count100"]}") + Double.Parse($"{play["count300"]}") + Double.Parse($"{play["countmiss"]}");
                    totalHits *= 300;
                    return Math.Round(hitPoints / totalHits * 100, 2);
                }
                else if (mode == 1)
                {
                    var hitPoints = Double.Parse($"{play["countmiss"]}") * 0 + Double.Parse($"{play["count100"]}") * 0.5 + Double.Parse($"{play["count300"]}") * 1;
                    var totalHits = Double.Parse($"{play["countmiss"]}") + Double.Parse($"{play["count100"]}") + Double.Parse($"{play["count300"]}");
                    hitPoints *= 300;
                    totalHits *= 300;
                    return Math.Round(hitPoints / totalHits * 100, 2);
                }
                else if (mode == 2)
                {
                    var fruitsCaught = Double.Parse($"{play["count50"]}") + Double.Parse($"{play["count100"]}") + Double.Parse($"{play["count300"]}");
                    var totalFruits = Double.Parse($"{play["countmiss"]}") + Double.Parse($"{play["count50"]}") + Double.Parse($"{play["count100"]}") + Double.Parse($"{play["count300"]}") + Double.Parse($"{play["countkatu"]}");
                    return Math.Round(fruitsCaught / totalFruits * 100, 2);
                }
                else
                {
                    var hitPoints = Double.Parse($"{play["count50"]}") * 50 + Double.Parse($"{play["count100"]}") * 100 + Double.Parse($"{play["countkatu"]}") * 200 + (Double.Parse($"{play["count300"]}") + Double.Parse($"{play["countgeki"]}")) * 300;
                    var totalHits = Double.Parse($"{play["countmiss"]}") + Double.Parse($"{play["count50"]}") + Double.Parse($"{play["count100"]}") + Double.Parse($"{play["countkatu"]}") + Double.Parse($"{play["count300"]}") + Double.Parse($"{play["countgeki"]}");
                    totalHits *= 300;
                    return Math.Round(hitPoints / totalHits * 100, 2);
                }
            }

            private static string ResolveMap(string mapLink)
            {
                Match s = new Regex(@"osu.ppy.sh\/s\/", RegexOptions.IgnoreCase).Match(mapLink);
                Match b = new Regex(@"osu.ppy.sh\/b\/", RegexOptions.IgnoreCase).Match(mapLink);
                Match p = new Regex(@"osu.ppy.sh\/p\/", RegexOptions.IgnoreCase).Match(mapLink);
                Match m = new Regex(@"&m=", RegexOptions.IgnoreCase).Match(mapLink);
                if (s.Success)
                {
                    var mapId = mapLink.Substring(mapLink.IndexOf("/s/") + 3);
                    return $"s={mapId}";
                }
                else if (b.Success)
                {
                    if (m.Success)
                        return $"b={mapLink.Substring(mapLink.IndexOf("/b/") + 3, mapLink.IndexOf("&m") - (mapLink.IndexOf("/b/") + 3))}";
                    else
                        return $"b={mapLink.Substring(mapLink.IndexOf("/b/") + 3)}";
                }
                else if (p.Success)
                {
                    if (m.Success)
                        return $"b={mapLink.Substring(mapLink.IndexOf("?b=") + 3, mapLink.IndexOf("&m") - (mapLink.IndexOf("?b=") + 3))}";
                    else
                        return $"b={mapLink.Substring(mapLink.IndexOf("?b=") + 3)}";
                }
                else
                {
                    return $"s={mapLink}"; //just a default incase an ID number was provided by itself (non-url)?
                }
            }

            private static int ResolveGameMode(string mode)
            {
                switch (mode.ToLower())
                {
                    case "std":
                    case "standard":
                        return 0;
                    case "taiko":
                        return 1;
                    case "ctb":
                    case "catchthebeat":
                        return 2;
                    case "mania":
                    case "osu!mania":
                        return 3;
                    default:
                        return 0;
                }
            }

            //https://github.com/ppy/osu-api/wiki#mods
            private static string ResolveMods(int mods)
            {
                var modString = $"+";

                if (IsBitSet(mods, 0))
                    modString += "NF";
                if (IsBitSet(mods, 1))
                    modString += "EZ";
                if (IsBitSet(mods, 8))
                    modString += "HT";

                if (IsBitSet(mods, 3))
                    modString += "HD";
                if (IsBitSet(mods, 4))
                    modString += "HR";
                if (IsBitSet(mods, 6) && !IsBitSet(mods, 9))
                    modString += "DT";
                if (IsBitSet(mods, 9))
                    modString += "NC";
                if (IsBitSet(mods, 10))
                    modString += "FL";

                if (IsBitSet(mods, 5))
                    modString += "SD";
                if (IsBitSet(mods, 14))
                    modString += "PF";

                if (IsBitSet(mods, 7))
                    modString += "RX";
                if (IsBitSet(mods, 11))
                    modString += "AT";
                if (IsBitSet(mods, 12))
                    modString += "SO";
                return modString;
            }

            private static bool IsBitSet(int mods, int pos) => 
                (mods & (1 << pos)) != 0;
        }
    }
}
