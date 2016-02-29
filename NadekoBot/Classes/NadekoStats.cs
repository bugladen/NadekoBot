using Discord;
using Discord.Commands;
using System;
using System.Diagnostics;
using System.Linq;
using NadekoBot.Extensions;
using System.Threading.Tasks;
using System.Reflection;

namespace NadekoBot {
    public class NadekoStats {
        public static NadekoStats Instance { get; } = new NadekoStats();

        private readonly CommandService commandService;

        public string BotVersion => $"{Assembly.GetExecutingAssembly().GetName().Name} v{Assembly.GetExecutingAssembly().GetName().Version}";

        private int _commandsRan = 0;
        private string _statsCache = "";
        private readonly Stopwatch statsStopwatch = new Stopwatch();

        public int ServerCount { get; private set; } = 0;
        public int TextChannelsCount { get; private set; } = 0;
        public int VoiceChannelsCount { get; private set; } = 0;

        static NadekoStats() { }

        private NadekoStats() {
            commandService = NadekoBot.Client.GetService<CommandService>();

            statsStopwatch = new Stopwatch();
            statsStopwatch.Start();
            commandService.CommandExecuted += StatsCollector_RanCommand;

            Task.Run(StartCollecting);
            Console.WriteLine("Logging enabled.");

            ServerCount = NadekoBot.Client.Servers.Count();
            var channels = NadekoBot.Client.Servers.SelectMany(s => s.AllChannels);
            TextChannelsCount = channels.Count(c => c.Type == ChannelType.Text);
            VoiceChannelsCount = channels.Count() - TextChannelsCount;

            NadekoBot.Client.JoinedServer += (s, e) => {
                try {
                    ServerCount++;
                    TextChannelsCount += e.Server.TextChannels.Count();
                    VoiceChannelsCount += e.Server.VoiceChannels.Count();
                }
                catch { }
            };
            NadekoBot.Client.LeftServer += (s, e) => {
                try {
                    ServerCount--;
                    TextChannelsCount -= e.Server.TextChannels.Count();
                    VoiceChannelsCount -= e.Server.VoiceChannels.Count();
                }
                catch { }
            };
            NadekoBot.Client.ChannelCreated += (s, e) => {
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
            NadekoBot.Client.ChannelDestroyed += (s, e) => {
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
                sb.AppendLine($"`Bot Version: {BotVersion}`");
                sb.AppendLine($"`Bot id: {NadekoBot.Client.CurrentUser.Id}`");
                sb.AppendLine($"`Owner id: {(NadekoBot.Creds.OwnerIds.FirstOrDefault())}`");
                sb.AppendLine($"`Uptime: {GetUptimeString()}`");
                sb.Append($"`Servers: {ServerCount}");
                sb.Append($" | TextChannels: {TextChannelsCount}");
                sb.AppendLine($" | VoiceChannels: {VoiceChannelsCount}`");
                sb.AppendLine($"`Commands Ran this session: {_commandsRan}`");
                sb.AppendLine($"`Message queue size:{NadekoBot.Client.MessageQueue.Count}`");
                sb.AppendLine($"`Greeted {Commands.ServerGreetCommand.Greeted} times.`");
                _statsCache = sb.ToString();
            });

        public string Heap() => Math.Round((double)GC.GetTotalMemory(true) / 1.MiB(), 2).ToString();

        public async Task<string> GetStats() {
            if (statsStopwatch.Elapsed.Seconds <= 5) return _statsCache;
            await LoadStats();
            statsStopwatch.Restart();
            return _statsCache;
        }

        private async Task StartCollecting() {
            while (true) {
                await Task.Delay(new TimeSpan(0, 30, 0));
                try {
                    var onlineUsers = await Task.Run(() => NadekoBot.Client.Servers.Sum(x => x.Users.Count()));
                    var realOnlineUsers = await Task.Run(() => NadekoBot.Client.Servers
                                                                        .Sum(x => x.Users.Count(u => u.Status == UserStatus.Online)));
                    var connectedServers = NadekoBot.Client.Servers.Count();

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
                Console.WriteLine("Error in ran command DB write.");
            }
        }
    }
}