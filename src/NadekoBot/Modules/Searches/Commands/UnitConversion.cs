using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
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
                    Units = JsonConvert.DeserializeObject<List<JsonUnit>>(File.ReadAllText("units.json"));
                }
                catch (Exception e)
                {
                    _log.Warn("Could not load units: " + e.Message);

                }

            }

            public List<JsonUnit> Units { get; set; }



            [Command("updatecur")]
            [RequireContext(ContextType.Guild)]
            public async Task UpdateCurrency(IUserMessage msg)
            {
                var channel = msg.Channel as IGuildChannel;
                var currencyRates = await UpdateCurrencyRates();
                var unitTypeString = "currency";
                var baseType = new JsonUnit()
                {
                    Triggers = new List<string>() { currencyRates.Base },
                    Modifier = decimal.One,
                    UnitType = unitTypeString
                };
                var baseIndex = Units.FindIndex(x => x.UnitType == "currency" && x.Modifier == baseType.Modifier);
                if (baseIndex == -1)
                    Units.Add(baseType);
                else
                    Units[baseIndex] = baseType;
                foreach (var rate in currencyRates.ConversionRates)
                {
                    var u = new JsonUnit()
                    {
                        Triggers = new List<string>() { rate.Key },
                        UnitType = unitTypeString,
                        Modifier = rate.Value
                    };
                    var lower = u.Triggers.First().ToLowerInvariant();
                    var toUpdate = Units.FindIndex(x => x.UnitType == "currency" && x.Triggers.First().ToLowerInvariant() == lower);
                    if (toUpdate == -1)
                        Units.Add(u);
                    else
                        Units[toUpdate] = u;
                }
                File.WriteAllText("units.json", JsonConvert.SerializeObject(Units, Formatting.Indented));
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
                    } else
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


        public class JsonUnit
        {
            public List<string> Triggers { get; set; }
            public string UnitType { get; set; }
            public decimal Modifier { get; set; }
        }


        #region GetXML
        /*
        public class UnitCollection
        {
            public List<UnitType> UnitTypes;

            public UnitCollection(string content)
            {
                using (var xmlReader = XmlReader.Create(File.OpenRead("units.xml"), new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = true }))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(xmlReader);

                    UnitTypes = new List<UnitType>();
                    foreach (XmlNode node in doc.LastChild.ChildNodes)
                    { //units/
                        UnitType type = new UnitType()
                        {
                            Name = node.Name
                        };
                        var units = new List<Unit>();
                        foreach (XmlNode unitNode in node.ChildNodes)
                        {
                            var curNode = unitNode.FirstChild;
                            Unit u = new Unit()
                            {
                                Key = curNode.InnerText,
                                Singular = (curNode = curNode.NextSibling).InnerText,
                                Plural = (curNode = curNode.NextSibling).InnerText,
                                Symbol = (curNode = curNode.NextSibling).InnerText,
                                Source = curNode.NextSibling.NextSibling.InnerText
                            };
                            List<Factor> factors = new List<Factor>();
                            foreach (XmlNode factorNode in curNode.NextSibling.ChildNodes)
                            {
                                Factor f = new Factor()
                                {
                                    Modifier = factorNode.FirstChild.InnerText.Replace(" ", "")
                                };
                                f.From = factorNode.Attributes.GetNamedItem("from").InnerText;
                                factors.Add(f);
                            }
                            u.Factors = factors;
                            units.Add(u);
                        }
                        type.Units = units;
                        UnitTypes.Add(type);
                    }
                }
            }

            public class UnitType
            {
                public string Name { get; set; }
                public List<Unit> Units { get; set; }
            }

            public class Unit
            {
                public string Key { get; set; }
                public string Plural { get; set; }
                public string Singular { get; set; }
                public string Symbol { get; set; }
                public List<Factor> Factors { get; set; }
                public string Source { get; set; }
            }

            public class Factor
            {
                public string From { get; set; }
                public string Modifier { get; set; }
            }
        }
        */
        #endregion
    }
}