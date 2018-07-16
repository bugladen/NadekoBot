using System;
using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Searches.Common;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class OverwatchCommands : NadekoSubmodule
        {
            public enum Region
            {
                Eu,
                Us,
                Kr
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Overwatch(Region region, [Remainder] string query = null)
            {
                if (string.IsNullOrWhiteSpace(query))
                    return;
                var battletag = query.Replace("#", "-", StringComparison.InvariantCulture);

                await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
                var model = (await GetProfile(region, battletag).ConfigureAwait(false))?.Stats;

                if (model != null)
                {
                    if (model.Competitive == null)
                    {
                        var qp = model.Quickplay;
                        var embed = new EmbedBuilder()
                            .WithAuthor(eau => eau.WithName(query)
                            .WithUrl($"https://www.overbuff.com/players/pc/{battletag}")
                            .WithIconUrl("https://cdn.discordapp.com/attachments/155726317222887425/255653487512256512/YZ4w2ey.png"))
                            .WithThumbnailUrl(qp.OverallStats.Avatar)
                            .AddField(fb => fb.WithName(GetText("level")).WithValue((qp.OverallStats.Level + (qp.OverallStats.Prestige * 100)).ToString()).WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("quick_wins")).WithValue(qp.OverallStats.Wins.ToString()).WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("compet_rank")).WithValue("0").WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("quick_playtime")).WithValue($"{qp.GameStats.TimePlayed}hrs").WithIsInline(true))
                            .WithOkColor();
                        await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                    }
                    else
                    {
                        var qp = model.Quickplay;
                        var compet = model.Competitive;
                        var embed = new EmbedBuilder()
                            .WithAuthor(eau => eau.WithName(query)
                                .WithUrl($"https://www.overbuff.com/players/pc/{battletag}")
                                .WithIconUrl(compet.OverallStats.RankImage))
                            .WithThumbnailUrl(compet.OverallStats.Avatar)
                            .AddField(fb => fb.WithName(GetText("level")).WithValue((qp.OverallStats.Level + (qp.OverallStats.Prestige * 100)).ToString()).WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("quick_wins")).WithValue(qp.OverallStats.Wins.ToString()).WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("compet_wins")).WithValue(compet.OverallStats.Wins.ToString()).WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("compet_loses")).WithValue(compet.OverallStats.Losses.ToString()).WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("compet_played")).WithValue(compet.OverallStats.Games.ToString() ?? "-").WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("compet_rank")).WithValue(compet.OverallStats.Comprank?.ToString() ?? "-").WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("compet_playtime")).WithValue(compet.GameStats.TimePlayed + "hrs").WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("quick_playtime")).WithValue(qp.GameStats.TimePlayed.ToString("F1") + "hrs").WithIsInline(true))
                            .WithColor(NadekoBot.OkColor);
                        await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                    }
                }
                else
                {
                    await ReplyErrorLocalized("ow_user_not_found").ConfigureAwait(false);
                }
            }
            public async Task<OverwatchApiModel.OverwatchPlayer> GetProfile(Region region, string battletag)
            {
                try
                {
                    using (var handler = new HttpClientHandler())
                    {
                        using (var http = new HttpClient(handler))
                        {
                            http.AddFakeHeaders();
                            var url = $"http://owapi.discord.how:2095/api/v3/u/{battletag}/blob?format=json_pretty";
                            var res = await http.GetStringAsync(url).ConfigureAwait(false);
                            var model = JsonConvert.DeserializeObject<OverwatchApiModel.OverwatchResponse>(res);
                            switch (region)
                            {
                                case Region.Eu:
                                    return model.Eu;
                                case Region.Kr:
                                    return model.Kr;
                                default:
                                    return model.Us;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    return null;
                }
            }
        }
    }
}