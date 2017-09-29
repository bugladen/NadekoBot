using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NadekoBot.Common.ShardCom
{
    public class ShardComServer : IDisposable
    {
        private readonly UdpClient _client;

        public ShardComServer(int port)
        {
            _client = new UdpClient(port);
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                var ip = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    var recv = await _client.ReceiveAsync();
                    var data = Encoding.UTF8.GetString(recv.Buffer);
                    var _ = OnDataReceived(JsonConvert.DeserializeObject<ShardComMessage>(data));
                }
            });
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public event Func<ShardComMessage, Task> OnDataReceived = delegate { return Task.CompletedTask; };
    }
}
