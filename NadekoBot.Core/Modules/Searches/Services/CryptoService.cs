using Discord;
using NadekoBot.Core.Modules.Searches.Common;
using NadekoBot.Core.Services;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Core.Modules.Searches.Services
{
    public class CryptoService : INService
    {
        private readonly SemaphoreSlim _cryptoLock = new SemaphoreSlim(1, 1);
        private readonly IDataCache _cache;
        private readonly IHttpClientFactory _httpFactory;

        public CryptoService(IDataCache cache, IHttpClientFactory httpFactory)
        {
            _cache = cache;
            _httpFactory = httpFactory;
        }

        public async Task<(CryptoData Data, CryptoData Nearest)> GetCryptoData(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (null, null);
            }

            name = name.ToUpperInvariant();
            var cryptos = await CryptoData().ConfigureAwait(false);

            var crypto = cryptos
                ?.FirstOrDefault(x => x.Id.ToUpperInvariant() == name || x.Name.ToUpperInvariant() == name
                    || x.Symbol.ToUpperInvariant() == name);

            (CryptoData Elem, int Distance)? nearest = null;
            if (crypto == null)
            {
                nearest = cryptos.Select(x => (x, Distance: x.Name.ToUpperInvariant().LevenshteinDistance(name)))
                    .OrderBy(x => x.Distance)
                    .Where(x => x.Distance <= 2)
                    .FirstOrDefault();

                crypto = nearest?.Elem;
            }

            if (nearest != null)
            {
                return (null, crypto);
            }

            return (crypto, null);
        }

        public async Task<CryptoData[]> CryptoData()
        {
            string data = null;
            var r = _cache.Redis.GetDatabase();
            await _cryptoLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var ver = await r.StringGetAsync("crypto_data_version").ConfigureAwait(false);
                if (ver != "2")
                {
                    await r.KeyDeleteAsync("crypto_data").ConfigureAwait(false);
                    await r.StringSetAsync("crypto_data_version", "2").ConfigureAwait(false);
                }

                data = await r.StringGetAsync("crypto_data").ConfigureAwait(false);

                if (data == null)
                {
                    var allData = new List<CryptoData>();
                    using (var http = _httpFactory.CreateClient())
                    {
                        for (int start = 0; start <= 400; start += 100)
                        {
                            data = await http.GetStringAsync(new Uri($"https://api.coinmarketcap.com/v2/ticker/?convert=BTC&start={start}"))
                                .ConfigureAwait(false);

                            allData.AddRange(JsonConvert.DeserializeObject<CryptoResponse>(data).Data.Select(x => x.Value).ToArray());
                        }
                    }
                    data = JsonConvert.SerializeObject(allData);
                    await r.StringSetAsync("crypto_data", data, TimeSpan.FromHours(1)).ConfigureAwait(false);
                }
            }
            finally
            {
                _cryptoLock.Release();
            }

            return JsonConvert.DeserializeObject<CryptoData[]>(data);
        }
    }
}
