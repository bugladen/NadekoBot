using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NadekoBot.Services;
using StackExchange.Redis;

namespace NadekoBot.Common.ShardCom
{
    public class ShardComClient
    {
        private readonly IDataCache _cache;

        public ShardComClient(IDataCache cache)
        {
            _cache = cache;
        }

        public async Task Send(ShardComMessage data)
        {
            var sub = _cache.Redis.GetSubscriber();
            var msg = JsonConvert.SerializeObject(data);

            await sub.PublishAsync("shardcoord_send", msg).ConfigureAwait(false);
        }
    }
}
