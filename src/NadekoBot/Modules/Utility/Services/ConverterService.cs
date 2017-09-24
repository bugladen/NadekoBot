using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using Newtonsoft.Json;
using NLog;

namespace NadekoBot.Modules.Utility.Services
{
    public class ConverterService : INService
    {
        public List<ConvertUnit> Units { get; } = new List<ConvertUnit>();
        private readonly Logger _log;
        private readonly Timer _currencyUpdater;
        private readonly TimeSpan _updateInterval = new TimeSpan(12, 0, 0);
        private readonly DbService _db;
        private readonly ConvertUnit[] fileData;

        public ConverterService(DiscordSocketClient client, DbService db)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;

            if (client.ShardId == 0)
            {
                try
                {
                    fileData = JsonConvert.DeserializeObject<List<MeasurementUnit>>(
                        File.ReadAllText("data/units.json"))
                            .Select(u => new ConvertUnit()
                            {
                                Modifier = u.Modifier,
                                UnitType = u.UnitType,
                                InternalTrigger = string.Join("|", u.Triggers)
                            }).ToArray();

                    using (var uow = _db.UnitOfWork)
                    {
                        if (uow.ConverterUnits.Empty())
                        {
                            uow.ConverterUnits.AddRange(fileData);

                            Units = uow.ConverterUnits.GetAll().ToList();
                            uow.Complete();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn("Could not load units: " + ex.Message);
                }
            }

            _currencyUpdater = new Timer(async (shouldLoad) => await UpdateCurrency((bool)shouldLoad), 
                client.ShardId == 0, 
                TimeSpan.FromSeconds(1), 
                _updateInterval);
        }

        private async Task<Rates> GetCurrencyRates()
        {
            using (var http = new HttpClient())
            {
                var res = await http.GetStringAsync("http://api.fixer.io/latest").ConfigureAwait(false);
                return JsonConvert.DeserializeObject<Rates>(res);
            }
        }

        private async Task UpdateCurrency(bool shouldLoad)
        {
            try
            {
                var unitTypeString = "currency";
                if (shouldLoad)
                {
                    var currencyRates = await GetCurrencyRates();
                    var baseType = new ConvertUnit()
                    {
                        Triggers = new[] { currencyRates.Base },
                        Modifier = decimal.One,
                        UnitType = unitTypeString
                    };
                    var range = currencyRates.ConversionRates.Select(u => new ConvertUnit()
                    {
                        InternalTrigger = u.Key,
                        Modifier = u.Value,
                        UnitType = unitTypeString
                    }).ToArray();
                    var toRemove = Units.Where(u => u.UnitType == unitTypeString);

                    using (var uow = _db.UnitOfWork)
                    {
                        if(toRemove.Any())
                            uow.ConverterUnits.RemoveRange(toRemove.ToArray());
                        uow.ConverterUnits.Add(baseType);
                        uow.ConverterUnits.AddRange(range);

                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    Units.RemoveAll(u => u.UnitType == unitTypeString);
                    Units.Add(baseType);
                    Units.AddRange(range);
                    Units.AddRange(fileData);
                }
                else
                {
                    using (var uow = _db.UnitOfWork)
                    {
                        Units.RemoveAll(u => u.UnitType == unitTypeString);
                        Units.AddRange(uow.ConverterUnits.GetAll().ToArray());
                    }
                }
            }
            catch
            {
                _log.Warn("Failed updating currency. Ignore this.");
            }
        }
    }

    public class MeasurementUnit
    {
        public List<string> Triggers { get; set; }
        public string UnitType { get; set; }
        public decimal Modifier { get; set; }
    }

    public class Rates
    {
        public string Base { get; set; }
        public DateTime Date { get; set; }
        [JsonProperty("rates")]
        public Dictionary<string, decimal> ConversionRates { get; set; }
    }
}
