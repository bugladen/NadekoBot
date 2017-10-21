using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Core.Services.Database.Models;
using NLog;

namespace NadekoBot.Modules.Utility.Common
{
    public class RepeatRunner
    {
        private readonly Logger _log;

        public Repeater Repeater { get; }
        public SocketGuild Guild { get; }
        public ITextChannel Channel { get; private set; }
        public TimeSpan InitialInterval { get; private set; }

        private IUserMessage oldMsg = null;
        private Timer _t;

        public RepeatRunner(DiscordSocketClient client, SocketGuild guild, Repeater repeater)
        {
            _log = LogManager.GetCurrentClassLogger();
            Repeater = repeater;
            Guild = guild;

            InitialInterval = Repeater.Interval;

            Run();
        }

        private void Run()
        {
            if (Repeater.StartTimeOfDay != null)
            {
                if ((InitialInterval = Repeater.StartTimeOfDay.Value - DateTime.UtcNow.TimeOfDay) < TimeSpan.Zero)
                    InitialInterval += TimeSpan.FromDays(1);
            }

            _t = new Timer(async (_) => {

                try { await Trigger().ConfigureAwait(false); } catch { }

            }, null, InitialInterval, Repeater.Interval);
        }

        public async Task Trigger()
        {
            var toSend = "🔄 " + Repeater.Message;
            //var lastMsgInChannel = (await Channel.GetMessagesAsync(2)).FirstOrDefault();
            // if (lastMsgInChannel.Id == oldMsg?.Id) //don't send if it's the same message in the channel
            //     continue;

            if (oldMsg != null)
                try
                {
                    await oldMsg.DeleteAsync();
                }
                catch
                {
                    // ignored
                }
            try
            {
                if (Channel == null)
                    Channel = Guild.GetTextChannel(Repeater.ChannelId);

                if (Channel != null)
                    oldMsg = await Channel.SendMessageAsync(toSend.SanitizeMentions()).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
            {
                _log.Warn("Missing permissions. Repeater stopped. ChannelId : {0}", Channel?.Id);
                return;
            }
            catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.NotFound)
            {
                _log.Warn("Channel not found. Repeater stopped. ChannelId : {0}", Channel?.Id);
                return;
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
            }
        }

        public void Reset()
        {
            Stop();
            Run();
        }

        public void Stop()
        {
            _t.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public override string ToString() =>
            $"{Channel?.Mention ?? $"⚠<#{Repeater.ChannelId}>" } " +
            $"| {(int)Repeater.Interval.TotalHours}:{Repeater.Interval:mm} " +
            $"| {Repeater.Message.TrimTo(33)}";
    }
}
