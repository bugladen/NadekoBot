using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.DataStructures.ShardCom
{
    public class ShardComClient
    {
        public async Task Send(ShardComMessage data)
        {
            var msg = JsonConvert.SerializeObject(data);
            using (var client = new UdpClient())
            {
                var bytes = Encoding.UTF8.GetBytes(msg);
                await client.SendAsync(bytes, bytes.Length, IPAddress.Loopback.ToString(), ShardComServer.Port).ConfigureAwait(false);
            }
        }
    }
}
