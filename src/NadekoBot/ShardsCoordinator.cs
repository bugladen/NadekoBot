using NadekoBot.DataStructures.ShardCom;
using NadekoBot.Services;
using NadekoBot.Services.Impl;
using NLog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot
{
    public class ShardsCoordinator
    {
        private readonly BotCredentials Credentials;
        private Process[] ShardProcesses;
        private ShardComMessage[] Statuses;
        private readonly Logger _log;
        private readonly ShardComServer _comServer;

        public ShardsCoordinator()
        {
            LogSetup.SetupLogger();
            Credentials = new BotCredentials();
            ShardProcesses = new Process[Credentials.TotalShards];
            Statuses = new ShardComMessage[Credentials.TotalShards];
            _log = LogManager.GetCurrentClassLogger();

            _comServer = new ShardComServer();
            _comServer.Start();

            _comServer.OnDataReceived += _comServer_OnDataReceived;
        }

        private Task _comServer_OnDataReceived(ShardComMessage msg)
        {
            Statuses[msg.ShardId] = msg;
            if (msg.ConnectionState == Discord.ConnectionState.Disconnected || msg.ConnectionState == Discord.ConnectionState.Disconnecting)
                _log.Error("!!! SHARD {0} IS IN {1} STATE", msg.ShardId, msg.ConnectionState);
            return Task.CompletedTask;
        }

        public async Task RunAsync()
        {
            var curProcessId = Process.GetCurrentProcess().Id;
            for (int i = 1; i < Credentials.TotalShards; i++)
            {
                var p = Process.Start(new ProcessStartInfo()
                {
                    FileName = "dotnet",
                    Arguments = $"run -c Debug -- {i} {curProcessId}",
                });
                await Task.Delay(5000);

                //Task.Run(() => { while (!p.HasExited) _log.Info($"S-{i}|" + p.StandardOutput.ReadLine()); });
                //Task.Run(() => { while (!p.HasExited) _log.Error($"S-{i}|" + p.StandardError.ReadLine()); });
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
            await Task.Run(() =>
            {
                string input;
                while ((input = Console.ReadLine()?.ToLowerInvariant()) != "quit")
                {
                    switch (input)
                    {
                        case "ls":
                            var groupStr = string.Join(",", Statuses
                                .Where(x => x != null)
                                .GroupBy(x => x.ConnectionState)
                                .Select(x => x.Count() + " " + x.Key));
                            _log.Info(string.Join("\n", Statuses.Select(x => $"Shard {x.ShardId} is in {x.ConnectionState.ToString()} state with {x.Guilds} servers")) + "\n" + groupStr);
                            break;
                        default:
                            break;
                    }
                }
            });
            foreach (var p in ShardProcesses)
            {
                try { p.Kill(); } catch { }
                try { p.Dispose(); } catch { }
            }
        }
    }
}
