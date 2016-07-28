using Discord.Commands;
using NadekoBot.Classes;
using ScaredFingers.UnitsConversion;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Searches.Commands
{
    class ConverterCommand : DiscordCommand
    {

        public ConverterCommand(DiscordModule module) : base(module)
        {
            if (unitTables == null)
            {
                CultureInfo ci = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentCulture = ci;
                unitTables = new List<UnitTable>();
                unitTables.Add(UnitTable.LengthTable);
                unitTables.Add(UnitTable.TemperatureTable);
                unitTables.Add(UnitTable.VolumeTable);
                unitTables.Add(UnitTable.WeightTable);
                reInitCurrencyConverterTable();
            }

        }


        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Module.Prefix + "convert")
                .Description($"Convert quantities from>to. | `{Prefix}convert m>km 1000`")
                .Parameter("from-to", ParameterType.Required)
                .Parameter("quantity", ParameterType.Optional)
                .Do(ConvertFunc());
            cgb.CreateCommand(Module.Prefix + "convertlist")
                .Description("List of the convertable dimensions and currencies.")
                .Do(ConvertListFunc());
        }

        private Func<CommandEventArgs, Task> ConvertListFunc() =>
            async e =>
            {
                reInitCurrencyConverterTable();
                string msg = "";
                foreach (var tmpTable in unitTables)
                {
                    int i = 1;
                    while (tmpTable.IsKnownUnit(i))
                    {
                        msg += tmpTable.GetUnitName(i) + " (" + tmpTable.GetUnitSymbol(i) + "); ";
                        i++;
                    }
                    msg += "\n";
                }
                foreach (var curr in exchangeRateProvider.Currencies)
                {
                    msg += curr + "; ";
                }

                await e.Channel.SendMessage(msg).ConfigureAwait(false);
            };

        private Func<CommandEventArgs, Task> ConvertFunc() =>
            async e =>
            {
                try
                {
                    await e.Channel.SendIsTyping().ConfigureAwait(false);

                    string from = e.GetArg("from-to").ToLowerInvariant().Split('>')[0];
                    string to = e.GetArg("from-to").ToLowerInvariant().Split('>')[1];

                    float quantity = 1.0f;
                    if (!float.TryParse(e.GetArg("quantity"), out quantity))
                    {
                        quantity = 1.0f;
                    }

                    int fromCode, toCode = 0;
                    UnitTable table = null;
                    ResolveUnitCodes(from, to, out table, out fromCode, out toCode);

                    if (table != null)
                    {
                        Unit inUnit = new Unit(fromCode, quantity, table);
                        Unit outUnit = inUnit.Convert(toCode);
                        await e.Channel.SendMessage(inUnit.ToString() + " = " + outUnit.ToString()).ConfigureAwait(false);
                    }
                    else
                    {
                        CultureInfo ci = new CultureInfo("en-US");
                        Thread.CurrentThread.CurrentCulture = ci;
                        reInitCurrencyConverterTable();
                        Unit inUnit = currTable.CreateUnit(quantity, from.ToUpperInvariant());
                        Unit outUnit = inUnit.Convert(currTable.CurrencyCode(to.ToUpperInvariant()));
                        await e.Channel.SendMessage(inUnit.ToString() + " = " + outUnit.ToString()).ConfigureAwait(false);
                    }
                }
                catch //(Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                    await e.Channel.SendMessage("Bad input format, or sth went wrong... Try to list them with `" + Module.Prefix + "`convertlist").ConfigureAwait(false);
                }
            };

        private void reInitCurrencyConverterTable()
        {
            if (lastChanged == null || lastChanged.DayOfYear != DateTime.Now.DayOfYear)
            {
                try
                {
                    exchangeRateProvider = new WebExchangeRatesProvider();
                    currTable = new CurrencyExchangeTable(exchangeRateProvider);
                    lastChanged = DateTime.Now;
                }
                catch
                {
                    Console.WriteLine("Error with the currency download.");
                }
            }
        }

        private void ResolveUnitCodes(string from, string to, out UnitTable table, out int fromCode, out int toCode)
        {
            foreach (var tmpTable in unitTables)
            {
                int f = LookupUnit(tmpTable, from);
                int t = LookupUnit(tmpTable, to);
                if (f > 0 && t > 0)
                {
                    table = tmpTable;
                    fromCode = f;
                    toCode = t;
                    return;
                }
            }
            table = null;
            fromCode = 0;
            toCode = 0;
        }

        private int LookupUnit(UnitTable table, string lookup)
        {
            string wellformedLookup = lookup.ToLowerInvariant().Replace("°", "");
            int i = 1;
            while (table.IsKnownUnit(i))
            {
                if (wellformedLookup == table.GetUnitName(i).ToLowerInvariant().Replace("°", "") ||
                    wellformedLookup == table.GetUnitPlural(i).ToLowerInvariant().Replace("°", "") ||
                    wellformedLookup == table.GetUnitSymbol(i).ToLowerInvariant().Replace("°", ""))
                {
                    return i;
                }
                i++;
            }
            return 0;
        }

        private static List<UnitTable> unitTables;

        private static CurrencyExchangeRatesProvider exchangeRateProvider;

        private static CurrencyExchangeTable currTable;

        private static DateTime lastChanged;
    }
}
