using NadekoBot.Services.Database.Models;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Services.Utility
{
    public class ConverterService
    {
        public List<ConvertUnit> Units { get; set; } = new List<ConvertUnit>();
        private readonly Logger _log;
        private Timer _timer;
        private readonly TimeSpan _updateInterval = new TimeSpan(12, 0, 0);
        private readonly DbHandler _db;

        public ConverterService(DbHandler db)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;
            try
            {
                var data = JsonConvert.DeserializeObject<List<MeasurementUnit>>(File.ReadAllText("data/units.json")).Select(u => new ConvertUnit()
                {
                    Modifier = u.Modifier,
                    UnitType = u.UnitType,
                    InternalTrigger = string.Join("|", u.Triggers)
                }).ToArray();

                using (var uow = _db.UnitOfWork)
                {
                    if (uow.ConverterUnits.Empty())
                    {
                        uow.ConverterUnits.AddRange(data);
                        uow.Complete();
                    }
                }
                Units = data.ToList();
            }
            catch (Exception ex)
            {
                _log.Warn("Could not load units: " + ex.Message);
            }

            _timer = new Timer(async (obj) => await UpdateCurrency(), null, _updateInterval, _updateInterval);
        }

        public static async Task<Rates> UpdateCurrencyRates()
        {
            using (var http = new HttpClient())
            {
                var res = await http.GetStringAsync("http://api.fixer.io/latest").ConfigureAwait(false);
                return JsonConvert.DeserializeObject<Rates>(res);
            }
        }

        public async Task UpdateCurrency()
        {
            try
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

                using (var uow = _db.UnitOfWork)
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
