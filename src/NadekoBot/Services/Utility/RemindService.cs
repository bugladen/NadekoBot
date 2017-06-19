using Discord;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Services.Utility
{
    public class RemindService
    {
        public readonly Regex Regex = new Regex(@"^(?:(?<months>\d)mo)?(?:(?<weeks>\d)w)?(?:(?<days>\d{1,2})d)?(?:(?<hours>\d{1,2})h)?(?:(?<minutes>\d{1,2})m)?$",
                                RegexOptions.Compiled | RegexOptions.Multiline);

        public string RemindMessageFormat { get; }

        public readonly IDictionary<string, Func<Reminder, string>> _replacements = new Dictionary<string, Func<Reminder, string>>
            {
                { "%message%" , (r) => r.Message },
                { "%user%", (r) => $"<@!{r.UserId}>" },
                { "%target%", (r) =>  r.IsPrivate ? "Direct Message" : $"<#{r.ChannelId}>"}
            };

        private readonly Logger _log;
        private readonly CancellationTokenSource cancelSource;
        private readonly CancellationToken cancelAllToken;
        private readonly BotConfig _config;
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;

        public RemindService(DiscordSocketClient client, BotConfig config, DbService db)
        {
            _config = config;
            _client = client;
            _log = LogManager.GetCurrentClassLogger();
            _db = db;

            cancelSource = new CancellationTokenSource();
            cancelAllToken = cancelSource.Token;
            List<Reminder> reminders;
            using (var uow = _db.UnitOfWork)
            {
                reminders = uow.Reminders.GetAll().ToList();
            }
            RemindMessageFormat = _config.RemindMessageFormat;

            foreach (var r in reminders)
            {
                Task.Run(() => StartReminder(r));
            }
        }

        public async Task StartReminder(Reminder r)
        {
            var t = cancelAllToken;
            var now = DateTime.UtcNow;

            var time = r.When - now;

            if (time.TotalMilliseconds > int.MaxValue)
                return;

            await Task.Delay(time, t).ConfigureAwait(false);
            try
            {
                IMessageChannel ch;
                if (r.IsPrivate)
                {
                    var user = _client.GetGuild(r.ServerId).GetUser(r.ChannelId);
                    if (user == null)
                        return;
                    ch = await user.CreateDMChannelAsync().ConfigureAwait(false);
                }
                else
                {
                    ch = _client.GetGuild(r.ServerId)?.GetTextChannel(r.ChannelId);
                }
                if (ch == null)
                    return;

                await ch.SendMessageAsync(
                    _replacements.Aggregate(RemindMessageFormat,
                        (cur, replace) => cur.Replace(replace.Key, replace.Value(r)))
                                             .SanitizeMentions()
                        ).ConfigureAwait(false); //it works trust me
            }
            catch (Exception ex) { _log.Warn(ex); }
            finally
            {
                using (var uow = _db.UnitOfWork)
                {
                    uow.Reminders.Remove(r);
                    await uow.CompleteAsync();
                }
            }
        }
    }
}