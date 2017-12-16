using NadekoBot.Extensions;
using NLog;
using StackExchange.Redis;
using System;
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
            Redis = ConnectionMultiplexer.Connect("127.0.0.1");
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
    }
}
