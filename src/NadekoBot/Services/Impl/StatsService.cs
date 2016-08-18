using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Impl
{
    public class StatsService : IStatsService
    {
        private int messageCounter;
        private DiscordSocketClient client;
        private DateTime started;

        public string BotVersion => "1.0-alpha";

        public string Heap => Math.Round((double)GC.GetTotalMemory(false) / 1.MiB(), 2).ToString();


        public StatsService(DiscordSocketClient client)
        {

            this.client = client;

            Reset();
            this.client.MessageReceived += _ => Task.FromResult(messageCounter++);

            this.client.Disconnected += _ => Reset();
        }
        public Task<string> Print() => Task.FromResult($@"`Author: Kwoth` `Library: Discord.Net`
`Bot Version: {BotVersion}`
`Bot id: {(client.GetCurrentUser()).Id}`
`Owners' Ids:`
`Uptime: {GetUptimeString()}`
`Servers: {client.GetGuilds().Count} | TextChannels: {client.GetGuilds().SelectMany(g => g.GetChannels().Where(c => c is ITextChannel)).Count()} | VoiceChannels: {client.GetGuilds().SelectMany(g => g.GetChannels().Where(c => c is IVoiceChannel)).Count()}`
`Messages: {messageCounter} ({messageCounter / (double)GetUptime().TotalSeconds:F2}/sec)` `Heap: {Heap} MB`");

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
