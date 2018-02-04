using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Impl;
using NLog;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace NadekoBot.Modules.Utility.Services
{
    public class RemindService : INService
    {
        public readonly Regex Regex = new Regex(@"^(?:(?<months>\d)mo)?(?:(?<weeks>\d)w)?(?:(?<days>\d{1,2})d)?(?:(?<hours>\d{1,2})h)?(?:(?<minutes>\d{1,2})m)?$",
                                RegexOptions.Compiled | RegexOptions.Multiline);

        public string RemindMessageFormat { get; }

        private readonly Logger _log;
        private readonly IBotConfigProvider _config;
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;

        public ConcurrentDictionary<int, Timer> Reminders { get; } = new ConcurrentDictionary<int, Timer>();

        public RemindService(DiscordSocketClient client, 
            IBotConfigProvider config, 
            DbService db,
            StartingGuildsService guilds)
        {
            _config = config;
            _client = client;
            _log = LogManager.GetCurrentClassLogger();
            _db = db;

            List<Reminder> reminders;
            using (var uow = _db.UnitOfWork)
            {
                reminders = uow.Reminders.GetIncludedReminders(guilds).ToList();
            }
            RemindMessageFormat = _config.BotConfig.RemindMessageFormat;

            foreach (var r in reminders)
            {
                StartReminder(r);
            }
        }

        public void StartReminder(Reminder r)
        {
            var time = r.When - DateTime.UtcNow;

            if (time.TotalMilliseconds > int.MaxValue)
                return;

            if (time.TotalMilliseconds < 0)
                time = TimeSpan.FromSeconds(5);
                
            var remT = new Timer(ReminderTimerAction, r, (int)time.TotalMilliseconds, Timeout.Infinite);
            if (!Reminders.TryAdd(r.Id, remT))
            {
                remT.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private async void ReminderTimerAction(object rObj)
        {
            var r = (Reminder)rObj;
                       
            try
            {
                IMessageChannel ch;
                if (r.IsPrivate)
                {
                    var user = _client.GetGuild(r.ServerId).GetUser(r.ChannelId);
                    if (user == null)
                        return;
                    ch = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);
                }
                else
                {
                    ch = _client.GetGuild(r.ServerId)?.GetTextChannel(r.ChannelId);
                }
                if (ch == null)
                    return;

                await ch.EmbedAsync(new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle("Reminder")
                    .AddField("Created At", r.DateAdded.HasValue ? r.DateAdded.Value.ToLongDateString() : "?")
                    .AddField("By", (await ch.GetUserAsync(r.UserId))?.ToString() ?? r.UserId.ToString()),
                    msg: r.Message.SanitizeMentions());
            }
            catch (Exception ex) { _log.Warn(ex); }
            finally
            {
                using (var uow = _db.UnitOfWork)
                {
                    uow.Reminders.Remove(r);
                    await uow.CompleteAsync();
                }
                var _ = Task.Run(() =>
                {
                    RemoveReminder(r.Id);
                });
            }
        }

        public void RemoveReminder(int id)
        {
            if (Reminders.TryRemove(id, out var t))
            {
                t.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
    }
}