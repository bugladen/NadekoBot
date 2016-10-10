using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Commands.Models;
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
using System.Xml;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class UnitConverterCommands
        {
            private Logger _log;
            private static Timer _timer;
            public static TimeSpan Span = new TimeSpan(12, 0, 0);
            public UnitConverterCommands()
            {
                _log = LogManager.GetCurrentClassLogger();

                try
                {
                    using (var uow = DbHandler.UnitOfWork())
                    {
                        //need to do this the first time                 
                        if (uow.ConverterUnits.Empty())
                        {
                            var content = JsonConvert.DeserializeObject<List<MeasurementUnit>>(File.ReadAllText("units.json")).Select(u => new ConvertUnit()
                            {
                                Modifier = u.Modifier,
                                UnitType = u.UnitType,
                                InternalTrigger = string.Join("|", u.Triggers)
                            });

                            uow.ConverterUnits.AddRange(content.ToArray());
                            uow.Complete();
                        }
                        Units = uow.ConverterUnits.GetAll().ToList();
                    }
                }
                catch (Exception e)
                {
                    _log.Warn("Could not load units: " + e.Message);
                }
                
                
                
                _timer = new Timer(new TimerCallback(UpdateCurrency), null, 0,(int)Span.TotalMilliseconds);

            }

            public void UpdateCurrency(object stateInfo)
            {
                var currencyRates = UpdateCurrencyRates().Result;
                var unitTypeString = "currency";
                using (var uow = DbHandler.UnitOfWork())
                {
                    var toRemove = Units.Where(u => u.UnitType == unitTypeString);
                    Units.RemoveAll(u => u.UnitType == unitTypeString);
                    uow.ConverterUnits.RemoveRange(toRemove.ToArray());
                    var baseType = new ConvertUnit()
                    {
                        Triggers = new[] { currencyRates.Base },
                        Modifier = decimal.One,
                        UnitType = unitTypeString
                    };
                    uow.ConverterUnits.Add(baseType);
                    Units.Add(baseType);
                    var range = currencyRates.ConversionRates.Select(u => new ConvertUnit()
                    {
                        InternalTrigger = u.Key,
                        Modifier = u.Value,
                        UnitType = unitTypeString
                    }).ToArray();
                    uow.ConverterUnits.AddRange(range);
                    Units.AddRange(range);

                    uow.Complete();
                }
                _log.Info("Updated Currency");
            }

            public List<ConvertUnit> Units { get; set; }
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ConvertList(IUserMessage msg)
            {
                var sb = new StringBuilder("Units that can be used by the converter: \n");
                var res = Units.GroupBy(x => x.UnitType);
                foreach (var group in res)
                {
                    sb.AppendLine($"{group.Key}: ```xl");
                    sb.AppendLine(string.Join(",", group.Select(x => x.Triggers.FirstOrDefault()).OrderBy(x => x)));
                    sb.AppendLine("```");
                }
                await msg.ReplyLong(sb.ToString(),  breakOn: new[] { "```xl\n", "\n" });
            }
            [NadekoCommand, Usage, Description, Aliases]
            public async Task Convert(IUserMessage msg, string origin, string target, double value)
            {
                var originUnit = Units.Find(x => x.Triggers.Select(y => y.ToLowerInvariant()).Contains(origin.ToLowerInvariant()));
                var targetUnit = Units.Find(x => x.Triggers.Select(y => y.ToLowerInvariant()).Contains(target.ToLowerInvariant()));
                if (originUnit == null || targetUnit == null)
                {
                    await msg.Reply(string.Format("Cannot convert {0} to {1}: units not found", origin, target));
                    return;
                }
                if (originUnit.UnitType != targetUnit.UnitType)
                {
                    await msg.Reply(string.Format("Cannot convert {0} to {1}: types of unit are not equal", originUnit.Triggers.First(), targetUnit.Triggers.First()));
                    return;
                }
                double res;
                if (originUnit.Triggers == targetUnit.Triggers) res = value;
                else if (originUnit.UnitType == "temperature")
                {
                    //don't really care too much about efficiency, so just convert to Kelvin, then to target
                    switch (originUnit.Triggers.First().ToUpperInvariant())
                    {
                        case "C":
                            res = value + 273.15; //celcius!
                            break;
                        case "F":
                            res = (value + 459.67) * (5 / 9);
                            break;
                        default:
                            res = value;
                            break;
                    }
                    //from Kelvin to target
                    switch (targetUnit.Triggers.First())
                    {
                        case "C":
                            res = value - 273.15; //celcius!
                            break;
                        case "F":
                            res = res * (9 / 5) - 458.67;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    if (originUnit.UnitType == "currency")
                    {
                        res = (value * (double)targetUnit.Modifier) / (double)originUnit.Modifier;
                    }
                    else
                        res = (value * (double)originUnit.Modifier) / (double)targetUnit.Modifier;
                }
                res = Math.Round(res, 2);

                await msg.Reply(string.Format("{0} {1}s is equal to {2} {3}s", value, originUnit.Triggers.First().SnPl(value.IsInteger() ? (int)value : 2), res, targetUnit.Triggers.First().SnPl(res.IsInteger() ? (int)res : 2)));
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