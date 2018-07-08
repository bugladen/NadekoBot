using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Core.Services.Database.Models;
using NLog;
using System.Linq;
using NadekoBot.Modules.Utility.Services;

namespace NadekoBot.Modules.Utility.Common
{
    public class RepeatRunner
    {
        private readonly Logger _log;

        public Repeater Repeater { get; }
        public SocketGuild Guild { get; }

        private readonly MessageRepeaterService _mrs;

        public ITextChannel Channel { get; private set; }
        public TimeSpan InitialInterval { get; private set; }

        private IUserMessage oldMsg = null;
        private Timer _t;

        public RepeatRunner(SocketGuild guild, Repeater repeater, MessageRepeaterService mrs)
        {
            _log = LogManager.GetCurrentClassLogger();
            Repeater = repeater;
            Guild = guild;
            _mrs = mrs;

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

            if (oldMsg != null && !Repeater.NoRedundant)
            {
                try
                {
                    await oldMsg.DeleteAsync().ConfigureAwait(false);
                    oldMsg = null;
                }
                catch
                {
                    // ignored
                }
            }

            try
            {
                if (Channel == null)
                    Channel = Guild.GetTextChannel(Repeater.ChannelId);


                if (Repeater.NoRedundant)
                {
                    var lastMsgInChannel = (await Channel.GetMessagesAsync(2).FlattenAsync().ConfigureAwait(false)).FirstOrDefault();
                    if (lastMsgInChannel != null && lastMsgInChannel.Id == oldMsg?.Id) //don't send if it's the same message in the channel
                        return;
                }

                if (Channel != null)
                    oldMsg = await Channel.SendMessageAsync(toSend.SanitizeMentions()).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
            {
                _log.Warn("Missing permissions. Repeater stopped. ChannelId : {0}", Channel?.Id);
                Stop();
                return;
            }
            catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.NotFound)
            {
                _log.Warn("Channel not found. Repeater stopped. ChannelId : {0}", Channel?.Id);
                Stop();
                await _mrs.RemoveRepeater(Repeater);
                return;
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
                Stop();
                await _mrs.RemoveRepeater(Repeater);
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
            (this.Repeater.NoRedundant ? "| ✍" : "") +
            $"| {(int)Repeater.Interval.TotalHours}:{Repeater.Interval:mm} " +
            $"| {Repeater.Message.TrimTo(33)}";
    }
}
