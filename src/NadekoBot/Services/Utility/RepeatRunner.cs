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

        private CancellationTokenSource source { get; set; }
        private CancellationToken token { get; set; }
        public Repeater Repeater { get; }
        public SocketGuild Guild { get; }
        public ITextChannel Channel { get; private set; }
        private IUserMessage oldMsg = null;

        public RepeatRunner(DiscordShardedClient client, Repeater repeater, ITextChannel channel = null)
        {
            _log = LogManager.GetCurrentClassLogger();
            Repeater = repeater;
            Channel = channel;

            //todo 40 @.@ fix all of this
            Guild = client.GetGuild(repeater.GuildId);
            if (Guild != null)
                Task.Run(Run);
        }

        private async Task Run()
        {
            source = new CancellationTokenSource();
            token = source.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(Repeater.Interval, token).ConfigureAwait(false);

                    await Trigger().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
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
            source.Cancel();
            var _ = Task.Run(Run);
        }

        public void Stop()
        {
            source.Cancel();
        }

        public override string ToString() =>
            $"{Channel?.Mention ?? $"⚠<#{Repeater.ChannelId}>" } " +
            $"| {(int)Repeater.Interval.TotalHours}:{Repeater.Interval:mm} " +
            $"| {Repeater.Message.TrimTo(33)}";
    }
}
