using Discord;
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
        private ShardedDiscordClient client;
        private DateTime started;

        public const string BotVersion = "1.1.0-beta";

        public string Author => "Kwoth#2560";
        public string Library => "Discord.Net";
        public int MessageCounter { get; private set; } = 0;
        public int CommandsRan { get; private set; } = 0;
        public string Heap =>
            Math.Round((double)GC.GetTotalMemory(false) / 1.MiB(), 2).ToString();
        public double MessagesPerSecond => MessageCounter / (double)GetUptime().TotalSeconds;
        private uint _textChannels = 0;
        public uint TextChannels => _textChannels;
        private uint _voiceChannels = 0;
        public uint VoiceChannels => _voiceChannels;
        public string OwnerIds => string.Join(", ", NadekoBot.Credentials.OwnerIds);

        Timer carbonitexTimer { get; }

        public StatsService(ShardedDiscordClient client, CommandHandler cmdHandler)
        {

            this.client = client;

            Reset();
            this.client.MessageReceived += _ => Task.FromResult(MessageCounter++);
            cmdHandler.CommandExecuted += (_, e) => Task.FromResult(CommandsRan++);

            this.client.Disconnected += _ => Reset();

            var guilds = this.client.GetGuilds();
            var _textChannels = guilds.Sum(g => g.Channels.Where(cx => cx is ITextChannel).Count());
            var _voiceChannels = guilds.Sum(g => g.Channels.Count) - _textChannels;

            this.client.ChannelCreated += (c) =>
            {
                if (c is ITextChannel)
                    ++_textChannels;
                else if (c is IVoiceChannel)
                    ++_voiceChannels;
            };

            this.client.ChannelDestroyed += (c) =>
            {
                if (c is ITextChannel)
                    --_textChannels;
                else if (c is IVoiceChannel)
                    --_voiceChannels;
            };

            this.client.JoinedGuild += (g) =>
            {
                var tc = g.Channels.Where(cx => cx is ITextChannel).Count();
                var vc = g.Channels.Count - tc;
                _textChannels += tc;
                _voiceChannels += vc;
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
                                { "servercount", this.client.GetGuildsCount().ToString() },
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
        public Task<string> Print()
        {
            var curUser = client.CurrentUser();
            return Task.FromResult($@"
Author: [{Author}] | Library: [{Library}]
Bot Version: [{BotVersion}]
Bot ID: {curUser.Id}
Owner ID(s): {OwnerIds}
Uptime: {GetUptimeString()}
Servers: {client.GetGuildsCount()} | TextChannels: {TextChannels} | VoiceChannels: {VoiceChannels}
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