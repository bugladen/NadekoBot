using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Models;
using Newtonsoft.Json;
using NLog;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class OverwatchCommands : ModuleBase
        {
            private readonly Logger _log;
            public OverwatchCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
            }
            [NadekoCommand, Usage, Description, Aliases]
            public async Task Overwatch(string region, [Remainder] string query = null)
            {
                if (string.IsNullOrWhiteSpace(query))
                    return;
                var battletag = Regex.Replace(query, "#", "-", RegexOptions.IgnoreCase);

                await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
                try
                {
                    await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
                    var model = await GetProfile(region, battletag);
                        
                    var rankimg = $"{model.Competitive.rank_img}";
                    var rank = $"{model.Competitive.rank}";
                    var competitiveplay = $"{model.Games.Competitive.played}";
                    if (string.IsNullOrWhiteSpace(rank))
                    {
                        var embed = new EmbedBuilder()
                            .WithAuthor(eau => eau.WithName($"{model.username}")
                            .WithUrl($"https://www.overbuff.com/players/pc/{battletag}")
                            .WithIconUrl($"{model.avatar}"))
                            .WithThumbnailUrl("https://cdn.discordapp.com/attachments/155726317222887425/255653487512256512/YZ4w2ey.png")
                            .AddField(fb => fb.WithName("**Level**").WithValue($"{model.level}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Quick Wins**").WithValue($"{model.Games.Quick.wins}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Current Competitive Wins**").WithValue($"{model.Games.Competitive.wins}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Current Competitive Loses**").WithValue($"{model.Games.Competitive.lost}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Current Competitive Played**").WithValue($"{model.Games.Competitive.played}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Competitive Rank**").WithValue("0").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Competitive Playtime**").WithValue($"{model.Playtime.competitive}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Quick Playtime**").WithValue($"{model.Playtime.quick}").WithIsInline(true))
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
                            .AddField(fb => fb.WithName("**Level**").WithValue($"{model.level}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Quick Wins**").WithValue($"{model.Games.Quick.wins}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Current Competitive Wins**").WithValue($"{model.Games.Competitive.wins}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Current Competitive Loses**").WithValue($"{model.Games.Competitive.lost}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Current Competitive Played**").WithValue($"{model.Games.Competitive.played}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Competitive Rank**").WithValue(rank).WithIsInline(true))
                            .AddField(fb => fb.WithName("**Competitive Playtime**").WithValue($"{model.Playtime.competitive}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Quick Playtime**").WithValue($"{model.Playtime.quick}").WithIsInline(true))
                            .WithColor(NadekoBot.OkColor);
                        await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                        return;
                    }
                }
                catch
                {
                    await Context.Channel.SendErrorAsync("Found no user! Please check the **Region** and **BattleTag** before trying again.");
                }
            }
            public async Task<OverwatchApiModel.OverwatchPlayer.Data> GetProfile(string region, string battletag)
            {
                try
                {
                    using (var http = new HttpClient())
                    {
                        var Url = await http.GetStringAsync($"https://api.lootbox.eu/pc/{region.ToLower()}/{battletag}/profile");
                        var model = JsonConvert.DeserializeObject<OverwatchApiModel.OverwatchPlayer>(Url);
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
