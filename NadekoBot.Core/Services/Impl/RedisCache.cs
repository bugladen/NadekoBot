using StackExchange.Redis;
using System.Threading.Tasks;

namespace NadekoBot.Services.Impl
{
    public class RedisCache : IDataCache
    {
        private ulong _botid;

        public ConnectionMultiplexer Redis { get; }
        private readonly IDatabase _db;

        public RedisCache(ulong botId)
        {
            _botid = botId;
            Redis = ConnectionMultiplexer.Connect("127.0.0.1");
            Redis.PreserveAsyncOrder = false;
            _db = Redis.GetDatabase();
        }

        public async Task<(bool Success, byte[] Data)> TryGetImageDataAsync(string key)
        {
            byte[] x = await _db.StringGetAsync("image_" + key);
            return (x != null, x);
        }

        public Task SetImageDataAsync(string key, byte[] data)
        {
            return _db.StringSetAsync("image_" + key, data);
        }

        public async Task<(bool Success, string Data)> TryGetAnimeDataAsync(string key)
        {
            string x = await _db.StringGetAsync("anime_" + key);
            return (x != null, x);
        }

        public Task SetAnimeDataAsync(string key, string data)
        {
            return _db.StringSetAsync("anime_" + key, data);
        }
    }
}
