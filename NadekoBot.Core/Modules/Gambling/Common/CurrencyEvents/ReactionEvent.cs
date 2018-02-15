//using Discord;
//using Discord.Commands;
//using Discord.WebSocket;
//using NadekoBot.Common.Collections;
//using NadekoBot.Extensions;
//using NadekoBot.Core.Services;
//using NadekoBot.Core.Services.Database.Models;
//using NLog;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Linq;

//namespace NadekoBot.Modules.Gambling.Common
//{
//    public class OldReactionEvent : ICurrencyEvent
//    {
//        private readonly ConcurrentHashSet<ulong> _reactionAwardedUsers = new ConcurrentHashSet<ulong>();
//        private readonly BotConfig _bc;
//        private readonly Logger _log;
//        private readonly DiscordSocketClient _client;
//        private readonly ICurrencyService _cs;
//        private readonly SocketSelfUser _botUser;

//        private IUserMessage StartingMessage { get; set; }

//        private CancellationTokenSource Source { get; }
//        private CancellationToken CancelToken { get; }

//        private readonly ConcurrentQueue<ulong> _toGiveTo = new ConcurrentQueue<ulong>();
//        private readonly long _amount;

//        public OldReactionEvent(BotConfig bc, DiscordSocketClient client, ICurrencyService cs, long amount)
//        {
//            _bc = bc;
//            _log = LogManager.GetCurrentClassLogger();
//            _client = client;
//            _cs = cs;
//            _botUser = client.CurrentUser;
//            _amount = amount;
//            Source = new CancellationTokenSource();
//            CancelToken = Source.Token;

//            var _ = Task.Run(async () =>
//            {
//                var users = new List<ulong>();
//                while (!CancelToken.IsCancellationRequested)
//                {
//                    await Task.Delay(1000).ConfigureAwait(false);
//                    while (_toGiveTo.TryDequeue(out var usrId))
//                    {
//                        users.Add(usrId);
//                    }

//                    if (users.Count > 0)
//                    {
//                        var usrs = users.ToArray();
//                        await _cs.AddBulkAsync(usrs, usrs.Select(x => "Reaction Event"), usrs.Select(x => _amount), gamble: true).ConfigureAwait(false);
//                    }

//                    users.Clear();
//                }
//            }, CancelToken);
//        }

//        public override async Task Stop()
//        {
//            if (StartingMessage != null)
//                await StartingMessage.DeleteAsync().ConfigureAwait(false);

//            if (!Source.IsCancellationRequested)
//                Source.Cancel();

//            _client.MessageDeleted -= MessageDeletedEventHandler;
//        }

//        private Task MessageDeletedEventHandler(Cacheable<IMessage, ulong> msg, ISocketMessageChannel channel)
//        {
//            if (StartingMessage?.Id == msg.Id)
//            {
//                _log.Warn("Stopping flower reaction event because message is deleted.");
//                var __ = Task.Run(Stop);
//            }

//            return Task.CompletedTask;
//        }

//        public override async Task Start(IUserMessage umsg, ICommandContext context)
//        {
//            StartingMessage = umsg;
//            _client.MessageDeleted += MessageDeletedEventHandler;

//            IEmote iemote;
//            if (Emote.TryParse(_bc.CurrencySign, out var emote))
//            {
//                iemote = emote;
//            }
//            else
//                iemote = new Emoji(_bc.CurrencySign);
//            try { await StartingMessage.AddReactionAsync(iemote).ConfigureAwait(false); }
//            catch
//            {
//                try { await StartingMessage.AddReactionAsync(iemote).ConfigureAwait(false); }
//                catch
//                {
//                    try { await StartingMessage.DeleteAsync().ConfigureAwait(false); }
//                    catch { return; }
//                }
//            }
//            using (StartingMessage.OnReaction(_client, (r) =>
//            {
//                try
//                {
//                    if ((r.User.IsSpecified && r.User.Value.IsBot) || r.UserId == _botUser.Id)
//                        return Task.CompletedTask;

//                    if (r.Emote.Name == iemote.Name && ((DateTime.UtcNow - r.User.Value.CreatedAt).TotalDays > 5) && _reactionAwardedUsers.Add(r.User.Value.Id))
//                    {
//                        _toGiveTo.Enqueue(r.UserId);
//                    }
//                }
//                catch
//                {
//                    // ignored
//                }
//                return Task.CompletedTask;
//            }))
//            {
//                try
//                {
//                    await Task.Delay(TimeSpan.FromHours(24), CancelToken).ConfigureAwait(false);
//                }
//                catch (OperationCanceledException)
//                {

//                }
//                if (CancelToken.IsCancellationRequested)
//                    return;

//                _log.Warn("Stopping flower reaction event because it expired.");
//                await Stop();
//            }
//        }
//    }
//}
