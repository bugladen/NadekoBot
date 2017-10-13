using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Common.Replacements;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Impl;
using NLog;
using System.Collections.Generic;

namespace NadekoBot.Modules.Utility.Services
{
    public class RemindService : INService
    {
        public readonly Regex Regex = new Regex(@"^(?:(?<months>\d)mo)?(?:(?<weeks>\d)w)?(?:(?<days>\d{1,2})d)?(?:(?<hours>\d{1,2})h)?(?:(?<minutes>\d{1,2})m)?$",
                                RegexOptions.Compiled | RegexOptions.Multiline);

        public string RemindMessageFormat { get; }

        private readonly Logger _log;
        private readonly CancellationTokenSource cancelSource;
        private readonly CancellationToken cancelAllToken;
        private readonly IBotConfigProvider _config;
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;

        public RemindService(DiscordSocketClient client, 
            IBotConfigProvider config, 
            DbService db,
            StartingGuildsService guilds)
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
                reminders = uow.Reminders.GetIncludedReminders(guilds).ToList();
            }
            RemindMessageFormat = _config.BotConfig.RemindMessageFormat;

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
                    ch = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);
                }
                else
                {
                    ch = _client.GetGuild(r.ServerId)?.GetTextChannel(r.ChannelId);
                }
                if (ch == null)
                    return;

                var rep = new ReplacementBuilder()
                    .WithOverride("%user%", () => $"<@!{r.UserId}>")
                    .WithOverride("%message%", () => r.Message)
                    .WithOverride("%target%", () => r.IsPrivate ? "Direct Message" : $"<#{r.ChannelId}>")
                    .Build();

                await ch.SendMessageAsync(rep.Replace(RemindMessageFormat).SanitizeMentions()).ConfigureAwait(false); //it works trust me
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