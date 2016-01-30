using Discord;
using Discord.Commands;
using Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using NadekoBot.Extensions;
using System.Threading.Tasks;

namespace NadekoBot
{
    public class NadekoStats
    {
        public string BotVersion = "0.8-beta3";

        private static readonly NadekoStats _instance = new NadekoStats();
        public static NadekoStats Instance => _instance;

        private CommandService _service;
        private DiscordClient _client;

        private int _commandsRan = 0;
        private string _statsCache = "";
        private Stopwatch _statsSW = new Stopwatch();

        List<string> messages = new List<string>();

        static NadekoStats() { }

        private NadekoStats() {
            _service = NadekoBot.client.Commands();
            _client = NadekoBot.client;

            _statsSW = new Stopwatch();
            _statsSW.Start();
            _service.CommandExecuted += StatsCollector_RanCommand;
            
            StartCollecting();
            Console.WriteLine("Logging enabled.");
        }

        public string GetUptimeString() {
            var time = (DateTime.Now - Process.GetCurrentProcess().StartTime);
            return time.Days + " days, " + time.Hours + " hours, and " + time.Minutes + " minutes.";
        }

        public void LoadStats() {
            _statsCache =
            "Author: Kwoth" +
            $"\nDiscord.Net version: {DiscordConfig.LibVersion}" +
            $"\nRuntime: {_client.GetRuntime()}" +
            $"\nBot Version: {BotVersion}" +
            $"\nLogged in as: {_client.CurrentUser.Name}" +
            $"\nBot id: {_client.CurrentUser.Id}" +
            $"\nUptime: {GetUptimeString()}" +
            $"\nServers: {_client.Servers.Count()}" +
            $"\nChannels: {_client.Servers.Sum(s => s.AllChannels.Count())}" +
            $"\nUsers: {_client.Servers.SelectMany(x => x.Users.Select(y => y.Id)).Count()} (non-unique)" +
            $"\nHeap: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString()}MB" +
            $"\nCommands Ran this session: {_commandsRan}";
        }

        public string GetStats() {
            if (_statsSW.ElapsedTicks > 5) {
                LoadStats();
                _statsSW.Restart();
            }
            return _statsCache;
        }

        private async Task StartCollecting() {
            while (true) {
                var obj = new ParseObject("Stats");
                obj["OnlineUsers"] = NadekoBot.client.Servers.Sum(x => x.Users.Count());
                obj["ConnectedServers"] = NadekoBot.client.Servers.Count();

                obj.SaveAsync();
                await Task.Delay(new TimeSpan(1, 0, 0));
            }
        }
        //todo - batch save this
        private void StatsCollector_RanCommand(object sender, CommandEventArgs e)
        {
            _commandsRan++;
            var obj = new ParseObject("CommandsRan");

            obj["ServerId"] = e.Server.Id;
            obj["ServerName"] = e.Server.Name;

            obj["ChannelId"] = e.Channel.Id;
            obj["ChannelName"] = e.Channel.Name;

            obj["UserId"] = e.User.Id;
            obj["UserName"] = e.User.Name;

            obj["CommandName"] = e.Command.Text;
            obj.SaveAsync();
        }
    }
}
