using Discord;
using NadekoBot.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        private class ChampionNameComparer : IEqualityComparer<JToken>
        {
            public bool Equals(JToken a, JToken b) => a["name"].ToString() == b["name"].ToString();

            public int GetHashCode(JToken obj) =>
                obj["name"].GetHashCode();
        }

        private static readonly string[] trashTalk = { "Better ban your counters. You are going to carry the game anyway.",
                                                "Go with the flow. Don't think. Just ban one of these.",
                                                "DONT READ BELOW! Ban Urgot mid OP 100%. Im smurf Diamond 1.",
                                                "Ask your teammates what would they like to play, and ban that.",
                                                "If you consider playing teemo, do it. If you consider teemo, you deserve him.",
                                                "Doesn't matter what you ban really. Enemy will ban your main and you will lose." };

        private static readonly Lazy<Dictionary<int, string>> champData = new Lazy<Dictionary<int, string>>(() => 
            ((IDictionary<string, JToken>)JObject.Parse(File.ReadAllText("data/lolchamps.json")))
                .ToDictionary(x => (int)x.Value["id"], x => x.Value["name"].ToString()), true);

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Lolban()
        {
            //const int showCount = 8;
            try
            {
                using (var http = new HttpClient())
                {
                    var data = JArray.Parse(await http.GetStringAsync($"http://api.champion.gg/v2/champions?champData=general&limit=200&api_key={_creds.LoLApiKey}"));

                    var champs = data.OrderByDescending(x => (double)x["banRate"]).Distinct(x => x["championId"]).Take(6);

                    //_log.Info(string.Join("\n", champs.Select(x => x["championId"] + " | " + x["banRate"] + " | " + x["normalized"]["banRate"])));
                    var eb = new EmbedBuilder().WithOkColor().WithTitle(Format.Underline(GetText("x_most_banned_champs", champs.Count())));
                    foreach (var champ in champs)
                    {
                        var lChamp = champ;
                        if (!champData.Value.TryGetValue((int)champ["championId"], out var champName))
                            champName = "UNKNOWN";
                        eb.AddField(efb => efb.WithName(champName).WithValue(((double)lChamp["banRate"] * 100).ToString("F2") + "%").WithIsInline(true));
                    }

                    await Context.Channel.EmbedAsync(eb, Format.Italics(trashTalk[new NadekoRandom().Next(0, trashTalk.Length)])).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
                await ReplyErrorLocalized("something_went_wrong").ConfigureAwait(false);
            }
        }
    }
}