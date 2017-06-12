using Discord;
using Discord.Net;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Services.Utility
{
    public class RepeatRunner
    {
        private readonly Logger _log;

        public Repeater Repeater { get; }
        public SocketGuild Guild { get; }
        public ITextChannel Channel { get; private set; }
        private IUserMessage oldMsg = null;
        private Timer _t;

        public RepeatRunner(DiscordShardedClient client, Repeater repeater, ITextChannel channel = null)
        {
            _log = LogManager.GetCurrentClassLogger();
            Repeater = repeater;
            Channel = channel;

            //todo 40 @.@ fix all of this
            Guild = client.GetGuild(repeater.GuildId);
            if (Guild != null)
                Run();
        }

        private void Run()
        {
            TimeSpan initialInterval = Repeater.Interval;

            //if (Repeater.StartTimeOfDay != null)
            //{
            //    if ((initialInterval = Repeater.StartTimeOfDay.Value - DateTime.UtcNow.TimeOfDay) < TimeSpan.Zero)
            //        initialInterval += TimeSpan.FromDays(1);
            //}
            
            _t = new Timer(async (_) => {

                try { await Trigger().ConfigureAwait(false); } catch { }

            }, null, initialInterval, Repeater.Interval);
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
