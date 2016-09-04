using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
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
            }

            public List<ConvertUnit> Units { get; set; }

            [Command("updatecur")]
            [RequireContext(ContextType.Guild)]
            public async Task UpdateCurrency(IUserMessage msg)
            {
                var channel = msg.Channel as IGuildChannel;
                var currencyRates = await UpdateCurrencyRates();
                var unitTypeString = "currency";
                var baseType = new ConvertUnit()
                {
                    Triggers = new[] { currencyRates.Base },
                    Modifier = decimal.One,
                    UnitType = unitTypeString
                };
                var baseIndex = Units.FindIndex(x => x.UnitType == "currency" && x.Modifier == baseType.Modifier);
                if (baseIndex == -1)
                    Units.Add(baseType);
                else
                    Units[baseIndex] = baseType;
                using (var uow = DbHandler.UnitOfWork())
                {
                    foreach (var rate in currencyRates.ConversionRates)
                    {
                        var u = new ConvertUnit()
                        {
                            Triggers = new[] { rate.Key },
                            UnitType = unitTypeString,
                            Modifier = rate.Value
                        };
                        var lower = u.Triggers.First().ToLowerInvariant();
                        var toUpdate = Units.FindIndex(x => x.UnitType == "currency" && x.Triggers.First().ToLowerInvariant() == lower);
                        if (toUpdate == -1)
                        {
                            Units.Add(u);
                            uow.ConverterUnits.Add(u);
                        }
                        else
                        {
                            Units[toUpdate] = u;
                            uow.ConverterUnits.Update(u);
                        }
                        uow.Complete();
                    }
                }
                await msg.Reply("done");
            }
            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            [RequireContext(ContextType.Guild)]
            public async Task ConvertListE(IUserMessage msg) //extended and bugged list
            {
                var channel = msg.Channel as IGuildChannel;

                var sb = new StringBuilder("Units that can be used by the converter: \n");
                var res = Units.GroupBy(x => x.UnitType);
                foreach (var group in res)
                {
                    sb.AppendLine($"{group.Key}: ```xl");
                    foreach (var el in group)
                    {
                        sb.Append($" [{string.Join(",", el.Triggers)}] ");
                    }
                    sb.AppendLine("```");
                }
                await msg.ReplyLong(sb.ToString(), "```xl", "```", "```xl");
            }
            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
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
                await msg.ReplyLong(sb.ToString(), "```xl", "```", "```xl");
            }
            [LocalizedCommand, LocalizedDescription, LocalizedSummary]
            public async Task Convert(IUserMessage msg, string origin, string target, decimal value)
            {
                var originUnit = Units.Find(x => x.Triggers.Select(y => y.ToLowerInvariant()).Contains(origin.ToLowerInvariant()));
                var targetUnit = Units.Find(x => x.Triggers.Select(y => y.ToLowerInvariant()).Contains(target.ToLowerInvariant()));
                if (originUnit == null || targetUnit == null)
                {
                    await msg.Reply(string.Format("Cannot convert {0} to {1}: units not found", originUnit.Triggers.First(), targetUnit.Triggers.First()));
                    return;
                }
                if (originUnit.UnitType != targetUnit.UnitType)
                {
                    await msg.Reply(string.Format("Cannot convert {0} to {1}: types of unit are not equal", originUnit.Triggers.First(), targetUnit.Triggers.First()));
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
                            res = value + (decimal)273.15; //celcius!
                            break;
                        case "F":
                            res = (value + (decimal)459.67) * ((decimal)5 / 9);
                            break;
                        default:
                            res = value;
                            break;
                    }
                    //from Kelvin to target
                    switch (targetUnit.Triggers.First())
                    {
                        case "C":
                            res = value - (decimal)273.15; //celcius!
                            break;
                        case "F":
                            res = res * ((decimal)9 / 5) - (decimal)458.67;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    //I just love currency
                    if (originUnit.UnitType == "currency")
                    {
                        res = (value * targetUnit.Modifier) / originUnit.Modifier;
                    }
                    else
                        res = (value * originUnit.Modifier) / targetUnit.Modifier;
                }
                res = Math.Round(res, 2);
                await msg.Reply(string.Format("{0} {1} is equal to {2} {3}", value, originUnit.Triggers.First(), res, targetUnit.Triggers.First()));
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

        public class Rates
        {
            [JsonProperty("base")]
            public string Base { get; set; }
            [JsonProperty("date")]
            public DateTime Date { get; set; }
            [JsonProperty("rates")]
            public Dictionary<string, decimal> ConversionRates { get; set; }
        }

        public class MeasurementUnit
        {
            public List<string> Triggers { get; set; }
            public string UnitType { get; set; }
            public decimal Modifier { get; set; }
        }
    }
}