using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Core.Services;
using Newtonsoft.Json;
using NLog;
using NadekoBot.Modules.Utility.Common;
using NadekoBot.Extensions;

namespace NadekoBot.Modules.Utility.Services
{
    public class ConverterService : INService, IUnloadableService
    {
        public ConvertUnit[] Units =>
            _cache.Redis.GetDatabase()
                .StringGet("converter_units")
                .ToString()
                .MapJson<ConvertUnit[]>();
                
        private readonly Logger _log;
        private readonly Timer _currencyUpdater;
        private readonly TimeSpan _updateInterval = new TimeSpan(12, 0, 0);
        private readonly DbService _db;
        private readonly IDataCache _cache;
        private readonly HttpClient _http;

        public ConverterService(DiscordSocketClient client, DbService db,
            IDataCache cache)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;
            _cache = cache;
            _http = new HttpClient();

            if (client.ShardId == 0)
            {
                _currencyUpdater = new Timer(async (shouldLoad) => await UpdateCurrency((bool)shouldLoad),
                    client.ShardId == 0,
                    TimeSpan.Zero,
                    _updateInterval);
            }
        }

        private async Task<Rates> GetCurrencyRates()
        {
            var res = await _http.GetStringAsync("http://api.fixer.io/latest").ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Rates>(res);
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
                        Triggers = new[] { u.Key },
                        Modifier = u.Value,
                        UnitType = unitTypeString
                    }).ToArray();

                    var fileData = JsonConvert.DeserializeObject<ConvertUnit[]>(
                            File.ReadAllText("data/units.json"))
                            .Where(x => x.UnitType != "currency");

                    var data = JsonConvert.SerializeObject(range.Append(baseType).Concat(fileData).ToList());
                    _cache.Redis.GetDatabase()
                        .StringSet("converter_units", data);
                }
            }
            catch
            {
                // ignored
            }
        }

        public Task Unload()
        {
            _currencyUpdater?.Change(Timeout.Infinite, Timeout.Infinite);
            return Task.CompletedTask;
        }
    }

    public class Rates
    {
        public string Base { get; set; }
        public DateTime Date { get; set; }
        [JsonProperty("rates")]
        public Dictionary<string, decimal> ConversionRates { get; set; }
    }
}
