using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Common;
using Newtonsoft.Json;
using NLog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public RedisCache(IBotCredentials creds)
        {
            _log = LogManager.GetCurrentClassLogger();
            var conf = ConfigurationOptions.Parse("127.0.0.1");
            conf.SyncTimeout = 3000;
            Redis = ConnectionMultiplexer.Connect(conf);
            Redis.PreserveAsyncOrder = false;
            LocalImages = new RedisImagesCache(Redis, creds);
            LocalData = new RedisLocalDataCache(Redis, creds);
            _redisKey = creds.RedisKey();
        }

        // things here so far don't need the bot id
        // because it's a good thing if different bots 
        // which are hosted on the same PC
        // can re-use the same image/anime data
        public async Task<(bool Success, byte[] Data)> TryGetImageDataAsync(string key)
        {
            var _db = Redis.GetDatabase();
            byte[] x = await _db.StringGetAsync("image_" + key);
            return (x != null, x);
        }

        public Task SetImageDataAsync(string key, byte[] data)
        {
            var _db = Redis.GetDatabase();
            return _db.StringSetAsync("image_" + key, data);
        }

        public async Task<(bool Success, string Data)> TryGetAnimeDataAsync(string key)
        {
            var _db = Redis.GetDatabase();
            string x = await _db.StringGetAsync("anime_" + key);
            return (x != null, x);
        }

        public Task SetAnimeDataAsync(string key, string data)
        {
            var _db = Redis.GetDatabase();
            return _db.StringSetAsync("anime_" + key, data);
        }

        public async Task<(bool Success, string Data)> TryGetNovelDataAsync(string key)
        {
            var _db = Redis.GetDatabase();
            string x = await _db.StringGetAsync("novel_" + key);
            return (x != null, x);
        }

        public Task SetNovelDataAsync(string key, string data)
        {
            var _db = Redis.GetDatabase();
            return _db.StringSetAsync("novel_" + key, data);
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
            var server = Redis.GetServer("127.0.0.1", 6379);
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
            return _db.StringSetAsync($"{_redisKey}_stream_{url}", data);
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
            var server = Redis.GetServer("127.0.0.1", 6379);
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
            var server = Redis.GetServer("127.0.0.1", 6379);
            var _db = Redis.GetDatabase();
            return Task.WhenAll(server.Keys(pattern: $"{_redisKey}_stream_*")
                .Select(x => _db.KeyDeleteAsync(x, CommandFlags.FireAndForget)));
        }

        public TimeSpan? TryAddRatelimit(ulong id, string name, int expireIn)
        {
            var _db = Redis.GetDatabase();
            if(_db.StringSet($"{_redisKey}_ratelimit_{id}_{name}",
                0, // i don't use the value
                TimeSpan.FromSeconds(expireIn),
                When.NotExists))
            {
                return null;
            }

            return _db.KeyTimeToLive($"{_redisKey}_ratelimit_{id}_{name}");
        }
    }
}
