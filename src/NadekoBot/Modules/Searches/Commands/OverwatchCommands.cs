using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Models;
using Newtonsoft.Json; 
using NLog;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class OverwatchCommands
        {
            private readonly Logger _log;
            public OverwatchCommands()
            {
                _log = LogManager.GetCurrentClassLogger();
            }
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Overwatch(IUserMessage umsg, string region, [Remainder] string query = null)
            {
                var channel = (ITextChannel)umsg.Channel;
                if (string.IsNullOrWhiteSpace(query))
                    return;
                var battletag = Regex.Replace(query, "#", "-", RegexOptions.IgnoreCase);

                await channel.TriggerTypingAsync().ConfigureAwait(false);
                try
                {
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
                            .WithThumbnail(th => th.WithUrl("https://cdn.discordapp.com/attachments/155726317222887425/255653487512256512/YZ4w2ey.png"))
                            .AddField(fb => fb.WithName("**Level**").WithValue($"{model.level}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Quick Wins**").WithValue($"{model.Games.Quick.wins}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Current Competitive Wins**").WithValue($"{model.Games.Competitive.wins}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Current Competitive Loses**").WithValue($"{model.Games.Competitive.lost}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Current Competitive Played**").WithValue($"{model.Games.Competitive.played}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Competitive Rank**").WithValue("0").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Competitive Playtime**").WithValue($"{model.Playtime.competitive}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Quick Playtime**").WithValue($"{model.Playtime.quick}").WithIsInline(true))
                            .WithOkColor();
                        await channel.EmbedAsync(embed.Build()).ConfigureAwait(false);
                    }
                    else
                    {
                        var embed = new EmbedBuilder()
                            .WithAuthor(eau => eau.WithName($"{model.username}")
                            .WithUrl($"https://www.overbuff.com/players/pc/{battletag}")
                            .WithIconUrl($"{model.avatar}"))
                            .WithThumbnail(th => th.WithUrl(rankimg))
                            .AddField(fb => fb.WithName("**Level**").WithValue($"{model.level}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Quick Wins**").WithValue($"{model.Games.Quick.wins}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Current Competitive Wins**").WithValue($"{model.Games.Competitive.wins}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Current Competitive Loses**").WithValue($"{model.Games.Competitive.lost}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Current Competitive Played**").WithValue($"{model.Games.Competitive.played}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Competitive Rank**").WithValue(rank).WithIsInline(true))
                            .AddField(fb => fb.WithName("**Competitive Playtime**").WithValue($"{model.Playtime.competitive}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Quick Playtime**").WithValue($"{model.Playtime.quick}").WithIsInline(true))
                            .WithOkColor();
                        await channel.EmbedAsync(embed.Build()).ConfigureAwait(false);
                    }
                    if (string.IsNullOrWhiteSpace(competitiveplay))
                    {
                        var embed = new EmbedBuilder()
                            .WithAuthor(eau => eau.WithName($"{model.username}")
                            .WithUrl($"https://www.overbuff.com/players/pc/{battletag}")
                            .WithIconUrl($"{model.avatar}"))
                            .WithThumbnail(th => th.WithUrl("https://cdn.discordapp.com/attachments/155726317222887425/255653487512256512/YZ4w2ey.png"))
                            .AddField(fb => fb.WithName("**Level**").WithValue($"{model.level}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Quick Wins**").WithValue($"{model.Games.Quick.wins}").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Competitive Playtime**").WithValue($"0 hour").WithIsInline(true))
                            .AddField(fb => fb.WithName("**Quick Playtime**").WithValue($"{model.Playtime.quick}").WithIsInline(true))
                            .WithOkColor();
                        await channel.EmbedAsync(embed.Build()).ConfigureAwait(false);
                    }
                }
                catch
                {
                    await channel.SendErrorAsync("Found no user! Please check the **Region** and **BattleTag** before trying again.");
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
