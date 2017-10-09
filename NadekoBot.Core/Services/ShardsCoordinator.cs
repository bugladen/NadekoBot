using NadekoBot.Services.Impl;
using NLog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.ShardCom;

namespace NadekoBot.Services
{
    public class ShardsCoordinator
    {
        private readonly BotCredentials _creds;
        private readonly Process[] _shardProcesses;
        public ShardComMessage[] Statuses { get; }
        public int GuildCount => Statuses.ToArray()
            .Where(x => x != null)
            .Sum(x => x.Guilds);

        private readonly Logger _log;
        private readonly ShardComServer _comServer;
        private readonly int _curProcessId;

        public ShardsCoordinator(IDataCache cache)
        {
            LogSetup.SetupLogger();
            _creds = new BotCredentials();
            _shardProcesses = new Process[_creds.TotalShards];
            Statuses = new ShardComMessage[_creds.TotalShards];

            for (int i = 0; i < Statuses.Length; i++)
            {
                Statuses[i] = new ShardComMessage();
                var s = Statuses[i];
                s.ConnectionState = Discord.ConnectionState.Disconnected;
                s.Guilds = 0;
                s.ShardId = i;
                s.Time = DateTime.Now - TimeSpan.FromMinutes(1);
            }

            _log = LogManager.GetCurrentClassLogger();

            _comServer = new ShardComServer(cache);
            _comServer.Start();

            _comServer.OnDataReceived += _comServer_OnDataReceived;

            _curProcessId = Process.GetCurrentProcess().Id;
        }

        private Task _comServer_OnDataReceived(ShardComMessage msg)
        {
            Statuses[msg.ShardId] = msg;
            if (msg.ConnectionState == Discord.ConnectionState.Disconnected || msg.ConnectionState == Discord.ConnectionState.Disconnecting)
                _log.Error("!!! SHARD {0} IS IN {1} STATE", msg.ShardId, msg.ConnectionState.ToString());
            return Task.CompletedTask;
        }

        public async Task RunAsync()
        {
            for (int i = 1; i < _creds.TotalShards; i++)
            {
                var p = Process.Start(new ProcessStartInfo()
                {
                    FileName = _creds.ShardRunCommand,
                    Arguments = string.Format(_creds.ShardRunArguments, i, _curProcessId, "")
                });
                // last "" in format is for backwards compatibility
                // because current startup commands have {2} in them probably
                await Task.Delay(5000);
            }
        }

        public async Task RunAndBlockAsync()
        {
            try
            {
                await RunAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            await Task.Delay(-1);
            foreach (var p in _shardProcesses)
            {
                try { p.Kill(); } catch { }
                try { p.Dispose(); } catch { }
            }
        }
    }
}
