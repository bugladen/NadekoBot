using StackExchange.Redis;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Impl
{
    public class RedisCache : IDataCache
    {
        public ConnectionMultiplexer Redis { get; }
        private readonly IDatabase _db;

        public RedisCache()
        {
            Redis = ConnectionMultiplexer.Connect("127.0.0.1");
            Redis.PreserveAsyncOrder = false;
            _db = Redis.GetDatabase();
        }

        // things here so far don't need the bot id
        // because it's a good thing if different bots 
        // which are hosted on the same PC
        // can re-use the same image/anime data
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
