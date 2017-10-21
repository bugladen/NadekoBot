using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Utility.Services;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class UnitConverterCommands : NadekoSubmodule<ConverterService>
        {
            [NadekoCommand, Usage, Description, Aliases]
            public async Task ConvertList()
            {
                var res = _service.Units.GroupBy(x => x.UnitType)
                               .Aggregate(new EmbedBuilder().WithTitle(GetText("convertlist"))
                                                            .WithColor(NadekoBot.OkColor),
                                          (embed, g) => embed.AddField(efb =>
                                                                         efb.WithName(g.Key.ToTitleCase())
                                                                         .WithValue(String.Join(", ", g.Select(x => x.Triggers.FirstOrDefault())
                                                                                                       .OrderBy(x => x)))));
                await Context.Channel.EmbedAsync(res);
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Convert(string origin, string target, decimal value)
            {
                var originUnit = _service.Units.FirstOrDefault(x => x.Triggers.Select(y => y.ToLowerInvariant()).Contains(origin.ToLowerInvariant()));
                var targetUnit = _service.Units.FirstOrDefault(x => x.Triggers.Select(y => y.ToLowerInvariant()).Contains(target.ToLowerInvariant()));
                if (originUnit == null || targetUnit == null)
                {
                    await ReplyErrorLocalized("convert_not_found", Format.Bold(origin), Format.Bold(target)).ConfigureAwait(false);
                    return;
                }
                if (originUnit.UnitType != targetUnit.UnitType)
                {
                    await ReplyErrorLocalized("convert_type_error", Format.Bold(originUnit.Triggers.First()), Format.Bold(targetUnit.Triggers.First())).ConfigureAwait(false);
                    return;
                }
                decimal res;
                if (originUnit.Triggers == targetUnit.Triggers) res = value;
                else if (originUnit.UnitType == "temperature")
                {
                    //don't really care too much about efficiency, so just convert to Kelvin, then to target
                    switch (originUnit.Triggers.First().ToUpperInvariant())
                    {
                        case "C":
                            res = value + 273.15m; //celcius!
                            break;
                        case "F":
                            res = (value + 459.67m) * (5m / 9m);
                            break;
                        default:
                            res = value;
                            break;
                    }
                    //from Kelvin to target
                    switch (targetUnit.Triggers.First().ToUpperInvariant())
                    {
                        case "C":
                            res = res - 273.15m; //celcius!
                            break;
                        case "F":
                            res = res * (9m / 5m) - 459.67m;
                            break;
                    }
                }
                else
                {
                    if (originUnit.UnitType == "currency")
                    {
                        res = (value * targetUnit.Modifier) / originUnit.Modifier;
                    }
                    else
                        res = (value * originUnit.Modifier) / targetUnit.Modifier;
                }
                res = Math.Round(res, 4);

                await Context.Channel.SendConfirmAsync(GetText("convert", value, (originUnit.Triggers.First()).SnPl(value.IsInteger() ? (int)value : 2), res, (targetUnit.Triggers.First() + "s").SnPl(res.IsInteger() ? (int)res : 2)));
            }
        }
    }
}