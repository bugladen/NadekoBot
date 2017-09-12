using StackExchange.Redis;
using System.Threading.Tasks;

namespace NadekoBot.Services.Impl
{
    public class RedisCache : IDataCache
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public RedisCache()
        {
            _redis = ConnectionMultiplexer.Connect("localhost");
            _db = _redis.GetDatabase();
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
    }
}
