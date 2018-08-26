using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Common;
using Newtonsoft.Json;
using NLog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Impl
{
    public class RedisCache : IDataCache
    {
        private readonly Logger _log;

        public ConnectionMultiplexer Redis { get; }

        public IImageCache LocalImages { get; }
        public ILocalDataCache LocalData { get; }

        private readonly string _redisKey;
        private readonly EndPoint _redisEndpoint;

        public RedisCache(IBotCredentials creds, int shardId)
        {
            _log = LogManager.GetCurrentClassLogger();

            var conf = ConfigurationOptions.Parse(creds.RedisOptions);

            Redis = ConnectionMultiplexer.Connect(conf);
            _redisEndpoint = Redis.GetEndPoints().First();
            Redis.PreserveAsyncOrder = false;
            LocalImages = new RedisImagesCache(Redis, creds);
            LocalData = new RedisLocalDataCache(Redis, creds, shardId);
            _redisKey = creds.RedisKey();
        }

        // things here so far don't need the bot id
        // because it's a good thing if different bots 
        // which are hosted on the same PC
        // can re-use the same image/anime data
        public async Task<(bool Success, byte[] Data)> TryGetImageDataAsync(Uri key)
        {
            var _db = Redis.GetDatabase();
            byte[] x = await _db.StringGetAsync("image_" + key).ConfigureAwait(false);
            return (x != null, x);
        }

        public Task SetImageDataAsync(Uri key, byte[] data)
        {
            var _db = Redis.GetDatabase();
            return _db.StringSetAsync("image_" + key, data);
        }

        public async Task<(bool Success, string Data)> TryGetAnimeDataAsync(string key)
        {
            var _db = Redis.GetDatabase();
            string x = await _db.StringGetAsync("anime_" + key).ConfigureAwait(false);
            return (x != null, x);
        }

        public Task SetAnimeDataAsync(string key, string data)
        {
            var _db = Redis.GetDatabase();
            return _db.StringSetAsync("anime_" + key, data, expiry: TimeSpan.FromHours(3));
        }

        public async Task<(bool Success, string Data)> TryGetNovelDataAsync(string key)
        {
            var _db = Redis.GetDatabase();
            string x = await _db.StringGetAsync("novel_" + key).ConfigureAwait(false);
            return (x != null, x);
        }

        public Task SetNovelDataAsync(string key, string data)
        {
            var _db = Redis.GetDatabase();
            return _db.StringSetAsync("novel_" + key, data, expiry: TimeSpan.FromHours(3));
        }

        private readonly object timelyLock = new object();
        public TimeSpan? AddTimelyClaim(ulong id, int period)
        {
            if (period == 0)
                return null;
            lock (timelyLock)
            {
                var time = TimeSpan.FromHours(period);
                var _db = Redis.GetDatabase();
                if ((bool?)_db.StringGet($"{_redisKey}_timelyclaim_{id}") == null)
                {
                    _db.StringSet($"{_redisKey}_timelyclaim_{id}", true, time);
                    return null;
                }
                return _db.KeyTimeToLive($"{_redisKey}_timelyclaim_{id}");
            }
        }

        public void RemoveAllTimelyClaims()
        {
            var server = Redis.GetServer(_redisEndpoint);
            var _db = Redis.GetDatabase();
            foreach (var k in server.Keys(pattern: $"{_redisKey}_timelyclaim_*"))
            {
                _db.KeyDelete(k, CommandFlags.FireAndForget);
            }
        }

        public bool TryAddAffinityCooldown(ulong userId, out TimeSpan? time)
        {
            var _db = Redis.GetDatabase();
            time = _db.KeyTimeToLive($"{_redisKey}_affinity_{userId}");
            if (time == null)
            {
                time = TimeSpan.FromMinutes(30);
                _db.StringSet($"{_redisKey}_affinity_{userId}", true, time);
                return true;
            }
            return false;
        }

        public bool TryAddDivorceCooldown(ulong userId, out TimeSpan? time)
        {
            var _db = Redis.GetDatabase();
            time = _db.KeyTimeToLive($"{_redisKey}_divorce_{userId}");
            if (time == null)
            {
                time = TimeSpan.FromHours(6);
                _db.StringSet($"{_redisKey}_divorce_{userId}", true, time);
                return true;
            }
            return false;
        }

        public Task SetStreamDataAsync(string url, string data)
        {
            var _db = Redis.GetDatabase();
            return _db.StringSetAsync($"{_redisKey}_stream_{url}", data, expiry: TimeSpan.FromHours(6));
        }

        public bool TryGetStreamData(string url, out string dataStr)
        {
            var _db = Redis.GetDatabase();
            dataStr = _db.StringGet($"{_redisKey}_stream_{url}");

            return !string.IsNullOrWhiteSpace(dataStr);
        }

        public void SubscribeToStreamUpdates(Func<StreamResponse[], Task> onStreamsUpdated)
        {
            var _sub = Redis.GetSubscriber();
            _sub.Subscribe($"{_redisKey}_stream_updates", (ch, msg) =>
            {
                onStreamsUpdated(JsonConvert.DeserializeObject<StreamResponse[]>(msg));
            });
        }

        public Task PublishStreamUpdates(List<StreamResponse> newStatuses)
        {
            var _sub = Redis.GetSubscriber();
            return _sub.PublishAsync($"{_redisKey}_stream_updates", JsonConvert.SerializeObject(newStatuses));
        }

        public async Task<StreamResponse[]> GetAllStreamDataAsync()
        {
            await Task.Yield();
            var server = Redis.GetServer(_redisEndpoint);
            var _db = Redis.GetDatabase();
            List<RedisValue> dataStrs = new List<RedisValue>();
            foreach (var k in server.Keys(pattern: $"{_redisKey}_stream_*"))
            {
                dataStrs.Add(_db.StringGet(k));
            }

            return dataStrs
                .Select(x => JsonConvert.DeserializeObject<StreamResponse>(x))
                .Where(x => !string.IsNullOrWhiteSpace(x.ApiUrl))
                .ToArray();
        }

        public Task ClearAllStreamData()
        {
            var server = Redis.GetServer(_redisEndpoint);
            var _db = Redis.GetDatabase();
            return Task.WhenAll(server.Keys(pattern: $"{_redisKey}_stream_*")
                .Select(x => _db.KeyDeleteAsync(x, CommandFlags.FireAndForget)));
        }

        public TimeSpan? TryAddRatelimit(ulong id, string name, int expireIn)
        {
            var _db = Redis.GetDatabase();
            if (_db.StringSet($"{_redisKey}_ratelimit_{id}_{name}",
                0, // i don't use the value
                TimeSpan.FromSeconds(expireIn),
                When.NotExists))
            {
                return null;
            }

            return _db.KeyTimeToLive($"{_redisKey}_ratelimit_{id}_{name}");
        }

        public bool TryGetEconomy(out string data)
        {
            var _db = Redis.GetDatabase();
            if ((data = _db.StringGet($"{_redisKey}_economy")) != null)
            {
                return true;
            }

            return false;
        }

        public void SetEconomy(string data)
        {
            var _db = Redis.GetDatabase();
            _db.StringSet($"{_redisKey}_economy",
                data,
                expiry: TimeSpan.FromMinutes(3));
        }

        public async Task<TOut> GetOrAddCachedDataAsync<TParam, TOut>(string key, Func<TParam, Task<TOut>> factory, TParam param, TimeSpan expiry)
        {
            var _db = Redis.GetDatabase();

            RedisValue data = await _db.StringGetAsync(key).ConfigureAwait(false);
            if (!data.HasValue)
            {
                var obj = await factory(param).ConfigureAwait(false);

                if (obj == default)
                    return default;

                await _db.StringSetAsync(key, JsonConvert.SerializeObject(obj),
                    expiry: expiry).ConfigureAwait(false);

                return obj;
            }
            return (TOut)JsonConvert.DeserializeObject(data, typeof(TOut));
        }
    }
}
