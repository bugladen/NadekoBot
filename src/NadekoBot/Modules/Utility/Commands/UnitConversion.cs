using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Utility.Commands.Models;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class UnitConverterCommands
        {

            public static List<ConvertUnit> Units { get; set; } = new List<ConvertUnit>();
            private static Logger _log { get; }
            private static Timer _timer;
            private static TimeSpan updateInterval = new TimeSpan(12, 0, 0);

            static UnitConverterCommands()
            {
                _log = LogManager.GetCurrentClassLogger();

                try
                {
                    var data = JsonConvert.DeserializeObject<List<MeasurementUnit>>(File.ReadAllText("data/units.json")).Select(u => new ConvertUnit()
                    {
                        Modifier = u.Modifier,
                        UnitType = u.UnitType,
                        InternalTrigger = string.Join("|", u.Triggers)
                    }).ToArray();

                    using (var uow = DbHandler.UnitOfWork())
                    {           
                        if (uow.ConverterUnits.Empty())
                        {
                            uow.ConverterUnits.AddRange(data);
                            uow.Complete();
                        }
                    }
                    Units = data.ToList();
                }
                catch (Exception e)
                {
                    _log.Warn("Could not load units: " + e.Message);
                }
            }

            public UnitConverterCommands()
            {
                _timer = new Timer(async (obj) => await UpdateCurrency(), null, (int)updateInterval.TotalMilliseconds, (int)updateInterval.TotalMilliseconds);

            }

            public async Task UpdateCurrency()
            {try
                {
                    var currencyRates = await UpdateCurrencyRates();
                    var unitTypeString = "currency";
                    var range = currencyRates.ConversionRates.Select(u => new ConvertUnit()
                    {
                        InternalTrigger = u.Key,
                        Modifier = u.Value,
                        UnitType = unitTypeString
                    }).ToArray();
                    var baseType = new ConvertUnit()
                    {
                        Triggers = new[] { currencyRates.Base },
                        Modifier = decimal.One,
                        UnitType = unitTypeString
                    };
                    var toRemove = Units.Where(u => u.UnitType == unitTypeString);

                    using (var uow = DbHandler.UnitOfWork())
                    {
                        uow.ConverterUnits.RemoveRange(toRemove.ToArray());
                        uow.ConverterUnits.Add(baseType);
                        uow.ConverterUnits.AddRange(range);

                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    Units.RemoveAll(u => u.UnitType == unitTypeString);
                    Units.Add(baseType);
                    Units.AddRange(range);
                    _log.Info("Updated Currency");
                }
                catch {
                    _log.Warn("Failed updating currency.");
                }
            }
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ConvertList(IUserMessage msg)
            {
                var res = Units.GroupBy(x => x.UnitType)
                               .Aggregate(new EmbedBuilder().WithTitle("__Units which can be used by the converter__")
                                                            .WithColor(NadekoBot.OkColor),
                                          (embed, g) => embed.AddField(efb => 
                                                                         efb.WithName(g.Key.ToTitleCase())
                                                                         .WithValue(String.Join(", ", g.Select(x => x.Triggers.FirstOrDefault())
                                                                                                       .OrderBy(x => x)))));
                await msg.Channel.EmbedAsync(res.Build());
            }
            [NadekoCommand, Usage, Description, Aliases]
            public async Task Convert(IUserMessage msg, string origin, string target, decimal value)
            {
                var originUnit = Units.Find(x => x.Triggers.Select(y => y.ToLowerInvariant()).Contains(origin.ToLowerInvariant()));
                var targetUnit = Units.Find(x => x.Triggers.Select(y => y.ToLowerInvariant()).Contains(target.ToLowerInvariant()));
                if (originUnit == null || targetUnit == null)
                {
                    await msg.Channel.SendErrorAsync(string.Format("Cannot convert {0} to {1}: units not found", origin, target));
                    return;
                }
                if (originUnit.UnitType != targetUnit.UnitType)
                {
                    await msg.Channel.SendErrorAsync(string.Format("Cannot convert {0} to {1}: types of unit are not equal", originUnit.Triggers.First(), targetUnit.Triggers.First()));
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
                        default:
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

                await msg.Channel.SendConfirmAsync(string.Format("{0} {1} is equal to {2} {3}", value, (originUnit.Triggers.First() + "s").SnPl(value.IsInteger() ? (int)value : 2), res, (targetUnit.Triggers.First() + "s").SnPl(res.IsInteger() ? (int)res : 2)));
            }
        }

        public static async Task<Rates> UpdateCurrencyRates()
        {
            using (var http = new HttpClient())
            {
                var res = await http.GetStringAsync("http://api.fixer.io/latest").ConfigureAwait(false);
                return JsonConvert.DeserializeObject<Rates>(res);
            }
        }
    }
}