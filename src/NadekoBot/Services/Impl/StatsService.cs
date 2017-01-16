using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Services.Impl
{
    public class StatsService : IStatsService
    {
        private DiscordShardedClient client;
        private DateTime started;

        public const string BotVersion = "1.1.2";

        public string Author => "Kwoth#2560";
        public string Library => "Discord.Net";
        public int MessageCounter { get; private set; } = 0;
        public int CommandsRan { get; private set; } = 0;
        public string Heap =>
            Math.Round((double)GC.GetTotalMemory(false) / 1.MiB(), 2).ToString();
        public double MessagesPerSecond => MessageCounter / GetUptime().TotalSeconds;
        private int _textChannels = 0;
        public int TextChannels => _textChannels;
        private int _voiceChannels = 0;
        public int VoiceChannels => _voiceChannels;

        Timer carbonitexTimer { get; }

        public StatsService(DiscordShardedClient client, CommandHandler cmdHandler)
        {

            this.client = client;

            Reset();
            this.client.MessageReceived += _ => Task.FromResult(MessageCounter++);
            cmdHandler.CommandExecuted += (_, e) => Task.FromResult(CommandsRan++);

            this.client.ChannelCreated += (c) =>
            {
                if (c is ITextChannel)
                    ++_textChannels;
                else if (c is IVoiceChannel)
                    ++_voiceChannels;

                return Task.CompletedTask;
            };

            this.client.ChannelDestroyed += (c) =>
            {
                if (c is ITextChannel)
                    --_textChannels;
                else if (c is IVoiceChannel)
                    --_voiceChannels;

                return Task.CompletedTask;
            };

            this.client.JoinedGuild += (g) =>
            {
                var tc = g.Channels.Where(cx => cx is ITextChannel).Count();
                var vc = g.Channels.Count - tc;
                _textChannels += tc;
                _voiceChannels += vc;

                return Task.CompletedTask;
            };

            this.client.LeftGuild += (g) =>
            {
                var tc = g.Channels.Where(cx => cx is ITextChannel).Count();
                var vc = g.Channels.Count - tc;
                _textChannels -= tc;
                _voiceChannels -= vc;

                return Task.CompletedTask;
            };

            this.carbonitexTimer = new Timer(async (state) =>
            {
                if (string.IsNullOrWhiteSpace(NadekoBot.Credentials.CarbonKey))
                    return;
                try
                {
                    using (var http = new HttpClient())
                    {
                        using (var content = new FormUrlEncodedContent(
                            new Dictionary<string, string> {
                                { "servercount", this.client.GetGuildCount().ToString() },
                                { "key", NadekoBot.Credentials.CarbonKey }}))
                        {
                            content.Headers.Clear();
                            content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                            var res = await http.PostAsync("https://www.carbonitex.net/discord/data/botdata.php", content).ConfigureAwait(false);
                        }
                    };
                }
                catch { }
            }, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        public void Initialize()
        {
            var guilds = this.client.GetGuilds();
            _textChannels = guilds.Sum(g => g.Channels.Where(cx => cx is ITextChannel).Count());
            _voiceChannels = guilds.Sum(g => g.Channels.Count) - _textChannels;
        }

        public Task<string> Print()
        {
            var curUser = client.CurrentUser;
            return Task.FromResult($@"
Author: [{Author}] | Library: [{Library}]
Bot Version: [{BotVersion}]
Bot ID: {curUser.Id}
Owner ID(s): {string.Join(", ", NadekoBot.Credentials.OwnerIds)}
Uptime: {GetUptimeString()}
Servers: {client.GetGuildCount()} | TextChannels: {TextChannels} | VoiceChannels: {VoiceChannels}
Commands Ran this session: {CommandsRan}
Messages: {MessageCounter} [{MessagesPerSecond:F2}/sec] Heap: [{Heap} MB]");
        }

        public Task Reset()
        {
            MessageCounter = 0;
            started = DateTime.Now;
            return Task.CompletedTask;
        }

        public TimeSpan GetUptime() =>
            DateTime.Now - started;

        public string GetUptimeString(string separator = ", ")
        {
            var time = GetUptime();
            return $"{time.Days} days{separator}{time.Hours} hours{separator}{time.Minutes} minutes";
        }
    }
}