using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.DataStructures.ShardCom
{
    public class ShardComServer : IDisposable
    {
        public const int Port = 5664;
        private readonly UdpClient _client = new UdpClient(Port);

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
