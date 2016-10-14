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
        private ShardedDiscordClient  client;
        private DateTime started;
        private int commandsRan = 0;

        public string BotVersion => "1.0-alpha";

        public string Heap => Math.Round((double)GC.GetTotalMemory(false) / 1.MiB(), 2).ToString();


        public StatsService(ShardedDiscordClient  client, CommandHandler cmdHandler)
        {

            this.client = client;

            Reset();
            this.client.MessageReceived += _ => Task.FromResult(messageCounter++);
            cmdHandler.CommandExecuted += (_, e) => commandsRan++;

            this.client.Disconnected += _ => Reset();
        }
        public Task<string> Print()
        {
            var curUser = client.GetCurrentUserAsync();
            return Task.FromResult($@"`Author: Kwoth` `Library: Discord.Net`
`Bot Version: {BotVersion}`
`Bot id: {curUser.Id}`
`Owners' Ids: {string.Join(", ", NadekoBot.Credentials.OwnerIds)}`
`Uptime: {GetUptimeString()}`
`Servers: {client.GetGuilds().Count} | TextChannels: {client.GetGuilds().SelectMany(g => g.GetChannels().Where(c => c is ITextChannel)).Count()} | VoiceChannels: {client.GetGuilds().SelectMany(g => g.GetChannels().Where(c => c is IVoiceChannel)).Count()}`
`Commands Ran this session: {commandsRan}`
`Messages: {messageCounter} ({messageCounter / (double)GetUptime().TotalSeconds:F2}/sec)` `Heap: {Heap} MB`");
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
