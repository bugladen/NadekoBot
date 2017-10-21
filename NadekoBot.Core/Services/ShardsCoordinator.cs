using NadekoBot.Core.Services.Impl;
using NLog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NadekoBot.Common.ShardCom;
using StackExchange.Redis;
using Newtonsoft.Json;
using NadekoBot.Extensions;

namespace NadekoBot.Core.Services
{
    public class ShardsCoordinator
    {
        private readonly BotCredentials _creds;
        private readonly string _key;
        private readonly Process[] _shardProcesses;

        private readonly Logger _log;
        private readonly int _curProcessId;
        private readonly ConnectionMultiplexer _redis;
        private ShardComMessage _defaultShardState;

        public ShardsCoordinator()
        {
            //load main stuff
            LogSetup.SetupLogger();
            _log = LogManager.GetCurrentClassLogger();
            _creds = new BotCredentials();

            _log.Info("Starting NadekoBot v" + StatsService.BotVersion);

            _key = _creds.RedisKey();
            _redis = ConnectionMultiplexer.Connect("127.0.0.1");

            //setup initial shard statuses
            _defaultShardState = new ShardComMessage()
            {
                ConnectionState = Discord.ConnectionState.Disconnected,
                Guilds = 0,
                Time = DateTime.Now - TimeSpan.FromMinutes(1)
            };
            var db = _redis.GetDatabase();
            _shardProcesses = new Process[_creds.TotalShards];
            for (int i = 0; i < _creds.TotalShards; i++)
            {
                _defaultShardState.ShardId = i;
                db.ListRightPush(_key + "_shardstats",
                    JsonConvert.SerializeObject(_defaultShardState),
                    flags: CommandFlags.FireAndForget);
            }

            _curProcessId = Process.GetCurrentProcess().Id;

            _redis = ConnectionMultiplexer.Connect("127.0.0.1");
            var sub = _redis.GetSubscriber();
            sub.Subscribe(_key + "_shardcoord_send", 
                OnDataReceived,
                CommandFlags.FireAndForget);

            sub.Subscribe(_key + "_shardcoord_restart",
                OnRestart,
                CommandFlags.FireAndForget);

            sub.Subscribe(_key + "_shardcoord_stop",
                OnStop,
                CommandFlags.FireAndForget);
        }

        private void OnStop(RedisChannel ch, RedisValue data)
        {
            var shardId = JsonConvert.DeserializeObject<int>(data);
            var db = _redis.GetDatabase();
            _defaultShardState.ShardId = shardId;
            db.ListSetByIndex(_key + "_shardstats",
                    shardId,
                    JsonConvert.SerializeObject(_defaultShardState),
                    CommandFlags.FireAndForget);
            var p = _shardProcesses[shardId];
            _shardProcesses[shardId] = null;
            try { p?.Kill(); } catch { }
            try { p?.Dispose(); } catch { }
        }

        private void OnRestart(RedisChannel ch, RedisValue data)
        {
            OnStop(ch, data);
            var shardId = JsonConvert.DeserializeObject<int>(data);
            _shardProcesses[shardId] = StartShard(shardId);
        }

        private void OnDataReceived(RedisChannel ch, RedisValue data)
        {
            var msg = JsonConvert.DeserializeObject<ShardComMessage>(data);
            if (msg == null)
                return;
            var db = _redis.GetDatabase();
            db.ListSetByIndex(_key + "_shardstats",
                    msg.ShardId,
                    data,
                    CommandFlags.FireAndForget);
            if (msg.ConnectionState == Discord.ConnectionState.Disconnected
                || msg.ConnectionState == Discord.ConnectionState.Disconnecting)
            {
                _log.Error("!!! SHARD {0} IS IN {1} STATE !!!", msg.ShardId, msg.ConnectionState.ToString());
            }
            return;
        }

        public async Task RunAsync()
        {
            int i = 0;
#if DEBUG
            i = 1;
#endif
            
            for (; i < _creds.TotalShards; i++)
            {
                var p = StartShard(i);

                _shardProcesses[i] = p;
                await Task.Delay(6000);
            }
        }

        private Process StartShard(int shardId)
        {
            return Process.Start(new ProcessStartInfo()
            {
                FileName = _creds.ShardRunCommand,
                Arguments = string.Format(_creds.ShardRunArguments, shardId, _curProcessId, "")
            });
            // last "" in format is for backwards compatibility
            // because current startup commands have {2} in them probably
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
                foreach (var p in _shardProcesses)
                {
                    try { p.Kill(); } catch { }
                    try { p.Dispose(); } catch { }
                }
                return;
            }

            await Task.Delay(-1);
        }
    }
}
