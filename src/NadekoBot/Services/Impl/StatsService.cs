using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Services.Impl
{
    public class StatsService : IStatsService
    {
        private int messageCounter;
        private ShardedDiscordClient  client;
        private DateTime started;
        private int commandsRan = 0;

        public const string BotVersion = "1.0-rc2";

        public string Heap => Math.Round((double)GC.GetTotalMemory(false) / 1.MiB(), 2).ToString();

        Timer carbonitexTimer { get; }


        public StatsService(ShardedDiscordClient  client, CommandHandler cmdHandler)
        {

            this.client = client;

            Reset();
            this.client.MessageReceived += _ => Task.FromResult(messageCounter++);
            cmdHandler.CommandExecuted += (_, e) => commandsRan++;

            this.client.Disconnected += _ => Reset();

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
                                { "servercount", this.client.GetGuilds().Count.ToString() },
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
        public async Task<string> Print()
        {
            var curUser = await client.GetCurrentUserAsync();
            return $@"`Author: Kwoth` `Library: Discord.Net`
`Bot Version: {BotVersion}`
`Bot id: {curUser.Id}`
`Owners' Ids: {string.Join(", ", NadekoBot.Credentials.OwnerIds)}`
`Uptime: {GetUptimeString()}`
`Servers: {client.GetGuilds().Count} | TextChannels: {client.GetGuilds().SelectMany(g => g.GetChannels().Where(c => c is ITextChannel)).Count()} | VoiceChannels: {client.GetGuilds().SelectMany(g => g.GetChannels().Where(c => c is IVoiceChannel)).Count()}`
`Commands Ran this session: {commandsRan}`
`Messages: {messageCounter} ({messageCounter / (double)GetUptime().TotalSeconds:F2}/sec)` `Heap: {Heap} MB`";
        }

        public Task Reset()
        {
            messageCounter = 0;
            started = DateTime.Now;
            return Task.CompletedTask;
        }

        public TimeSpan GetUptime() =>
            DateTime.Now - started;

        public string GetUptimeString()
        {
            var time = GetUptime();
            return time.Days + " days, " + time.Hours + " hours, and " + time.Minutes + " minutes.";
        }
    }
}
