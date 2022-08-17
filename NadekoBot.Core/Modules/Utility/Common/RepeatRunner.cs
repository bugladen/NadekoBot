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

            _t = new Timer(async (_) =>
            {

                try { await Trigger().ConfigureAwait(false); } catch { }

            }, null, InitialInterval, Repeater.Interval);
        }

        public async Task Trigger()
        {
            async Task ChannelMissingError()
            {
                _log.Warn("Channel not found or insufficient permissions. Repeater stopped. ChannelId : {0}", Channel?.Id);
                Stop();
                await _mrs.RemoveRepeater(Repeater);
            }

            var toSend = "🔄 " + Repeater.Message;
            try
            {
                Channel = Channel ?? Guild.GetTextChannel(Repeater.ChannelId);

                if (Channel == null)
                {
                    await ChannelMissingError().ConfigureAwait(false);
                    return;
                }

                if (Repeater.NoRedundant)
                {
                    var lastMsgInChannel = (await Channel.GetMessagesAsync(2).FlattenAsync().ConfigureAwait(false)).FirstOrDefault();
                    if (lastMsgInChannel != null && lastMsgInChannel.Id == Repeater.LastMessageId) //don't send if it's the same message in the channel
                        return;
                }

                // if the message needs to be send
                // delete previous message if it exists
                try
                {
                    if (Repeater.LastMessageId != null)
                    {
                        var oldMsg = await Channel.GetMessageAsync(Repeater.LastMessageId.Value).ConfigureAwait(false);
                        if (oldMsg != null)
                        {
                            await oldMsg.DeleteAsync().ConfigureAwait(false);
                            oldMsg = null;
                        }
                    }
                }
                catch
                {
                    // ignored
                }

                var newMsg = await Channel.SendMessageAsync(toSend.SanitizeMentions()).ConfigureAwait(false);

                if (Repeater.NoRedundant)
                {
                    _mrs.SetRepeaterLastMessage(Repeater.Id, newMsg.Id);
                    Repeater.LastMessageId = newMsg.Id;
                }
            }
            catch (HttpException ex)
            {
                _log.Warn(ex.Message);
                await ChannelMissingError().ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
                Stop();
                await _mrs.RemoveRepeater(Repeater).ConfigureAwait(false);
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
