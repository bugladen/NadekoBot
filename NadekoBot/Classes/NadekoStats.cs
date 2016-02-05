using Discord;
using Discord.Commands;
using Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NadekoBot.Extensions;
using System.Threading.Tasks;

namespace NadekoBot
{
    public class NadekoStats
    {
        public string BotVersion = "0.8-beta6";

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
            "`Author: Kwoth`" +
            //$"\nDiscord.Net version: {DiscordConfig.LibVersion}" +
            //$"\nRuntime: {_client.GetRuntime()}" +
            $"\n`Bot Version: {BotVersion}`" +
            //$"\nLogged in as: {_client.CurrentUser.Name}" +
            $"\n`Bot id: {_client.CurrentUser.Id}`" +
            $"\n`Uptime: {GetUptimeString()}`" +
            $"\n`Servers: {_client.Servers.Count()}`" +
            $"\n`Channels: {_client.Servers.Sum(s => s.AllChannels.Count())}`" +
            //$"\nUsers: {_client.Servers.SelectMany(x => x.Users.Select(y => y.Id)).Count()} (non-unique)" +
            $"\n`Heap: {Math.Round((double)GC.GetTotalMemory(true) / 1.MiB(), 2).ToString()} MB`" +
            $"\n`Commands Ran this session: {_commandsRan}`" +
            $"\n`Greeted/Byed {Commands.ServerGreetCommand.Greeted} times.`";
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
                await Task.Delay(new TimeSpan(1, 0, 0));
                try {
                    var obj = new ParseObject("Stats");
                    obj["OnlineUsers"] = await Task.Run(() => NadekoBot.client.Servers.Sum(x => x.Users.Count()));
                    obj["RealOnlineUsers"] = await Task.Run(() => NadekoBot
                                                                        .client.Servers
                                                                        .Sum(x => x.Users.Where(u => u.Status == UserStatus.Online).Count()));
                    obj["ConnectedServers"] = NadekoBot.client.Servers.Count();

                    await obj.SaveAsync();
                } catch (Exception) {
                    Console.WriteLine("Parse exception in StartCollecting");
                    break;
                }
            }
        }
        //todo - batch save this
        private void StatsCollector_RanCommand(object sender, CommandEventArgs e)
        {
            try {
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
            } catch (Exception) {
                Console.WriteLine("Parse error in ran command.");
            }
        }
    }
}
