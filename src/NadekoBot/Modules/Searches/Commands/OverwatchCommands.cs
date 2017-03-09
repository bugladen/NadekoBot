using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class OverwatchCommands : NadekoSubmodule
        {
            [NadekoCommand, Usage, Description, Aliases]
            public async Task Overwatch(string region, [Remainder] string query = null)
            {
                if (string.IsNullOrWhiteSpace(query))
                    return;
                var battletag = Regex.Replace(query, "#", "-");

                await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
                try
                {
                    await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
                    var model = await GetProfile(region, battletag);

                    var rankimg = model.Competitive.rank_img;
                    var rank = model.Competitive.rank;

                    if (string.IsNullOrWhiteSpace(rank))
                    {
                        var embed = new EmbedBuilder()
                            .WithAuthor(eau => eau.WithName($"{model.username}")
                            .WithUrl($"https://www.overbuff.com/players/pc/{battletag}")
                            .WithIconUrl($"{model.avatar}"))
                            .WithThumbnailUrl("https://cdn.discordapp.com/attachments/155726317222887425/255653487512256512/YZ4w2ey.png")
                            .AddField(fb => fb.WithName(GetText("level")).WithValue($"{model.level}").WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("quick_wins")).WithValue($"{model.Games.Quick.wins}").WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("compet_rank")).WithValue("0").WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("quick_playtime")).WithValue($"{model.Playtime.quick}").WithIsInline(true))
                            .WithOkColor();
                        await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                    }
                    else
                    {
                        var embed = new EmbedBuilder()
                            .WithAuthor(eau => eau.WithName($"{model.username}")
                            .WithUrl($"https://www.overbuff.com/players/pc/{battletag}")
                            .WithIconUrl($"{model.avatar}"))
                            .WithThumbnailUrl(rankimg)
                            .AddField(fb => fb.WithName(GetText("level")).WithValue($"{model.level}").WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("quick_wins")).WithValue($"{model.Games.Quick.wins}").WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("compet_wins")).WithValue($"{model.Games.Competitive.wins}").WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("compet_losses")).WithValue($"{model.Games.Competitive.lost}").WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("compet_played")).WithValue($"{model.Games.Competitive.played}").WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("compet_rank")).WithValue(rank).WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("compet_played")).WithValue($"{model.Playtime.competitive}").WithIsInline(true))
                            .AddField(fb => fb.WithName(GetText("quick_playtime")).WithValue($"{model.Playtime.quick}").WithIsInline(true))
                            .WithColor(NadekoBot.OkColor);
                        await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                    }
                }
                catch
                {
                    await ReplyErrorLocalized("ow_user_not_found").ConfigureAwait(false);
                }
            }
            public async Task<OverwatchApiModel.OverwatchPlayer.Data> GetProfile(string region, string battletag)
            {
                try
                {
                    using (var http = new HttpClient())
                    {
                        var url = await http.GetStringAsync($"https://api.lootbox.eu/pc/{region.ToLower()}/{battletag}/profile");
                        var model = JsonConvert.DeserializeObject<OverwatchApiModel.OverwatchPlayer>(url);
                        return model.data;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}