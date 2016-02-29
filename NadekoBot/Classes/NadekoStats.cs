using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NadekoBot.Extensions;
using System.Threading.Tasks;
using System.Reflection;



namespace NadekoBot {
    public class NadekoStats {
        public string BotVersion { get; } = $"{Assembly.GetExecutingAssembly().GetName().Name} v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";

        private static readonly NadekoStats _instance = new NadekoStats();
        public static NadekoStats Instance => _instance;

        private CommandService _service;
        private DiscordClient _client;

        private int _commandsRan = 0;
        private string _statsCache = "";
        private Stopwatch _statsSW = new Stopwatch();

        public int ServerCount { get; private set; } = 0;
        public int TextChannelsCount { get; private set; } = 0;
        public int VoiceChannelsCount { get; private set; } = 0;

        List<string> messages = new List<string>();

        static NadekoStats() { }

        private NadekoStats() {
            _service = NadekoBot.client.GetService<CommandService>();
            _client = NadekoBot.client;

            _statsSW = new Stopwatch();
            _statsSW.Start();
            _service.CommandExecuted += StatsCollector_RanCommand;

            Task.Run(() => StartCollecting());
            Console.WriteLine("Logging enabled.");

            ServerCount = _client.Servers.Count();
            var channels = _client.Servers.SelectMany(s => s.AllChannels);
            TextChannelsCount = channels.Where(c => c.Type == ChannelType.Text).Count();
            VoiceChannelsCount = channels.Count() - TextChannelsCount;

            _client.JoinedServer += (s, e) => {
                try {
                    ServerCount++;
                    TextChannelsCount += e.Server.TextChannels.Count();
                    VoiceChannelsCount += e.Server.VoiceChannels.Count();
                }
                catch { }
            };
            _client.LeftServer += (s, e) => {
                try {
                    ServerCount--;
                    TextChannelsCount -= e.Server.TextChannels.Count();
                    VoiceChannelsCount -= e.Server.VoiceChannels.Count();
                }
                catch { }
            };
            _client.ChannelCreated += (s, e) => {
                try {
                    if (e.Channel.IsPrivate)
                        return;
                    if (e.Channel.Type == ChannelType.Text)
                        TextChannelsCount++;
                    else if (e.Channel.Type == ChannelType.Voice)
                        VoiceChannelsCount++;
                }
                catch { }
            };
            _client.ChannelDestroyed += (s, e) => {
                try {
                    if (e.Channel.IsPrivate)
                        return;
                    if (e.Channel.Type == ChannelType.Text)
                        VoiceChannelsCount++;
                    else if (e.Channel.Type == ChannelType.Voice)
                        VoiceChannelsCount--;
                }
                catch { }
            };
        }

        public TimeSpan GetUptime() =>
            DateTime.Now - Process.GetCurrentProcess().StartTime;

        public string GetUptimeString() {
            var time = (DateTime.Now - Process.GetCurrentProcess().StartTime);
            return time.Days + " days, " + time.Hours + " hours, and " + time.Minutes + " minutes.";
        }

        public Task LoadStats() =>
            Task.Run(() => {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("`Author: Kwoth` `Library: Discord.Net`");
                //$"\nDiscord.Net version: {DiscordConfig.LibVersion}" +
                //$"\nRuntime: {_client.GetRuntime()}" +
                sb.AppendLine($"`Bot Version: {BotVersion}`");
                //$"\nLogged in as: {_client.CurrentUser.Name}" +
                sb.AppendLine($"`Bot id: {_client.CurrentUser.Id}`");
                sb.AppendLine($"`Owner id: {NadekoBot.OwnerID}`");
                sb.AppendLine($"`Uptime: {GetUptimeString()}`");
                sb.Append($"`Servers: {ServerCount}");
                sb.Append($" | TextChannels: {TextChannelsCount}");
                sb.AppendLine($" | VoiceChannels: {VoiceChannelsCount}`");
                //$"\nUsers: {_client.Servers.SelectMany(x => x.Users.Select(y => y.Id)).Count()} (non-unique)" +
                //sb.AppendLine($"`Heap: {} MB`");
                sb.AppendLine($"`Commands Ran this session: {_commandsRan}`");
                sb.AppendLine($"`Message queue size:{_client.MessageQueue.Count}`");
                sb.AppendLine($"`Greeted {Commands.ServerGreetCommand.Greeted} times.`");
                _statsCache = sb.ToString();
            });

        public string Heap() => Math.Round((double)GC.GetTotalMemory(true) / 1.MiB(), 2).ToString();

        public async Task<string> GetStats() {
            if (_statsSW.Elapsed.Seconds > 5) {
                await LoadStats();
                _statsSW.Restart();
            }
            return _statsCache;
        }

        private async Task StartCollecting() {
            while (true) {
                await Task.Delay(new TimeSpan(0, 30, 0));
                try {
                    var onlineUsers = await Task.Run(() => NadekoBot.client.Servers.Sum(x => x.Users.Count()));
                    var realOnlineUsers = await Task.Run(() => NadekoBot.client.Servers
                                                                        .Sum(x => x.Users.Where(u => u.Status == UserStatus.Online).Count()));
                    var connectedServers = NadekoBot.client.Servers.Count();

                    Classes.DBHandler.Instance.InsertData(new Classes._DataModels.Stats {
                        OnlineUsers = onlineUsers,
                        RealOnlineUsers = realOnlineUsers,
                        Uptime = GetUptime(),
                        ConnectedServers = connectedServers,
                        DateAdded = DateTime.Now
                    });
                }
                catch {
                    Console.WriteLine("DB Exception in stats collecting.");
                    break;
                }
            }
        }
        //todo - batch save this
        private void StatsCollector_RanCommand(object sender, CommandEventArgs e) {
            try {
                _commandsRan++;
                Classes.DBHandler.Instance.InsertData(new Classes._DataModels.Command {
                    ServerId = (long)e.Server.Id,
                    ServerName = e.Server.Name,
                    ChannelId = (long)e.Channel.Id,
                    ChannelName = e.Channel.Name,
                    UserId = (long)e.User.Id,
                    UserName = e.User.Name,
                    CommandName = e.Command.Text,
                    DateAdded = DateTime.Now
                });
            }
            catch {
                Console.WriteLine("Parse error in ran command.");
            }
        }
    }
}