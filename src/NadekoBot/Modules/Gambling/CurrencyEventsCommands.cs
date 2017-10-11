using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Threading;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Collections;
using NLog;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class CurrencyEventsCommands : NadekoSubmodule
        {
            public enum CurrencyEvent
            {
                Reaction,
                SneakyGameStatus
            }
            //flower reaction event
            private static readonly ConcurrentHashSet<ulong> _sneakyGameAwardedUsers = new ConcurrentHashSet<ulong>();
            
            private static readonly char[] _sneakyGameStatusChars = Enumerable.Range(48, 10)
                .Concat(Enumerable.Range(65, 26))
                .Concat(Enumerable.Range(97, 26))
                .Select(x => (char)x)
                .ToArray();

            private string _secretCode = string.Empty;
            private readonly DiscordSocketClient _client;
            private readonly IBotConfigProvider _bc;
            private readonly CurrencyService _cs;

            public CurrencyEventsCommands(DiscordSocketClient client, IBotConfigProvider bc, CurrencyService cs)
            {
                _client = client;
                _bc = bc;
                _cs = cs;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task StartEvent(CurrencyEvent e, int arg = -1)
            {
                switch (e)
                {
                    case CurrencyEvent.Reaction:
                        await ReactionEvent(Context, arg).ConfigureAwait(false);
                        break;
                    case CurrencyEvent.SneakyGameStatus:
                        await SneakyGameStatusEvent(Context, arg).ConfigureAwait(false);
                        break;
                }
            }

            public async Task SneakyGameStatusEvent(ICommandContext context, int? arg)
            {
                int num;
                if (arg == null || arg < 5)
                    num = 60;
                else
                    num = arg.Value;

                if (_secretCode != string.Empty)
                    return;
                var rng = new NadekoRandom();

                for (var i = 0; i < 5; i++)
                {
                    _secretCode += _sneakyGameStatusChars[rng.Next(0, _sneakyGameStatusChars.Length)];
                }
                
                await _client.SetGameAsync($"type {_secretCode} for " + _bc.BotConfig.CurrencyPluralName)
                    .ConfigureAwait(false);
                try
                {
                    var title = GetText("sneakygamestatus_title");
                    var desc = GetText("sneakygamestatus_desc", Format.Bold(100.ToString()) + _bc.BotConfig.CurrencySign, Format.Bold(num.ToString()));
                    await context.Channel.SendConfirmAsync(title, desc).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }


                _client.MessageReceived += SneakyGameMessageReceivedEventHandler;
                await Task.Delay(num * 1000);
                _client.MessageReceived -= SneakyGameMessageReceivedEventHandler;

                var cnt = _sneakyGameAwardedUsers.Count;
                _sneakyGameAwardedUsers.Clear();
                _secretCode = string.Empty;

                await _client.SetGameAsync(GetText("sneakygamestatus_end", cnt))
                    .ConfigureAwait(false);
            }

            private Task SneakyGameMessageReceivedEventHandler(SocketMessage arg)
            {
                if (arg.Content == _secretCode &&
                    _sneakyGameAwardedUsers.Add(arg.Author.Id))
                {
                    var _ = Task.Run(async () =>
                    {
                        await _cs.AddAsync(arg.Author, "Sneaky Game Event", 100, false)
                            .ConfigureAwait(false);

                        try { await arg.DeleteAsync(new RequestOptions() { RetryMode = RetryMode.AlwaysFail }).ConfigureAwait(false); }
                        catch
                        {
                            // ignored
                        }
                    });
                }

                return Task.CompletedTask;
            }

            public async Task ReactionEvent(ICommandContext context, int amount)
            {
                if (amount <= 0)
                    amount = 100;

                var title = GetText("reaction_title");
                var desc = GetText("reaction_desc", _bc.BotConfig.CurrencySign, Format.Bold(amount.ToString()) + _bc.BotConfig.CurrencySign);
                var footer = GetText("reaction_footer", 24);
                var msg = await context.Channel.SendConfirmAsync(title,
                        desc, footer: footer)
                    .ConfigureAwait(false);

                await new ReactionEvent(_bc.BotConfig, _client, _cs, amount).Start(msg, context);
            }
        }
    }

    public abstract class CurrencyEvent
    {
        public abstract Task Start(IUserMessage msg, ICommandContext channel);
    }

    public class ReactionEvent : CurrencyEvent
    {
        private readonly ConcurrentHashSet<ulong> _reactionAwardedUsers = new ConcurrentHashSet<ulong>();
        private readonly BotConfig _bc;
        private readonly Logger _log;
        private readonly DiscordSocketClient _client;
        private readonly CurrencyService _cs;
        private readonly SocketSelfUser _botUser;

        private IUserMessage StartingMessage { get; set; }

        private CancellationTokenSource Source { get; }
        private CancellationToken CancelToken { get; }

        private readonly ConcurrentQueue<ulong> _toGiveTo = new ConcurrentQueue<ulong>();
        private readonly int _amount;

        public ReactionEvent(BotConfig bc, DiscordSocketClient client, CurrencyService cs, int amount)
        {
            _bc = bc;
            _log = LogManager.GetCurrentClassLogger();
            _client = client;
            _cs = cs;
            _botUser = client.CurrentUser;
            _amount = amount;
            Source = new CancellationTokenSource();
            CancelToken = Source.Token;

            var _ = Task.Run(async () =>
            {

                var users = new List<ulong>();
                while (!CancelToken.IsCancellationRequested)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    while (_toGiveTo.TryDequeue(out var usrId))
                    {
                        users.Add(usrId);
                    }

                    if (users.Count > 0)
                    {
                        await _cs.AddToManyAsync("Reaction Event", _amount, users.ToArray()).ConfigureAwait(false);
                    }

                    users.Clear();
                }
            }, CancelToken);
        }

        private async Task End()
        {
            if(StartingMessage != null)
                await StartingMessage.DeleteAsync().ConfigureAwait(false);

            if(!Source.IsCancellationRequested)
                Source.Cancel();

            _client.MessageDeleted -= MessageDeletedEventHandler;
        }

        private Task MessageDeletedEventHandler(Cacheable<IMessage, ulong> msg, ISocketMessageChannel channel) {
            if (StartingMessage?.Id == msg.Id)
            {
                _log.Warn("Stopping flower reaction event because message is deleted.");
                var __ = Task.Run(End);
            }

            return Task.CompletedTask;
        }

        public override async Task Start(IUserMessage umsg, ICommandContext context)
        {
            StartingMessage = umsg;
            _client.MessageDeleted += MessageDeletedEventHandler;

            IEmote iemote;
            if (Emote.TryParse(_bc.CurrencySign, out var emote))
            {
                iemote = emote;
            }
            else
                iemote = new Emoji(_bc.CurrencySign);
            try { await StartingMessage.AddReactionAsync(iemote).ConfigureAwait(false); }
            catch
            {
                try { await StartingMessage.AddReactionAsync(iemote).ConfigureAwait(false); }
                catch
                {
                    try { await StartingMessage.DeleteAsync().ConfigureAwait(false); }
                    catch { return; }
                }
            }
            using (StartingMessage.OnReaction(_client, (r) =>
            {
                try
                {
                    if (r.UserId == _botUser.Id)
                        return;

                    if (r.Emote.Name == iemote.Name && r.User.IsSpecified && ((DateTime.UtcNow - r.User.Value.CreatedAt).TotalDays > 5) && _reactionAwardedUsers.Add(r.User.Value.Id))
                    {
                        _toGiveTo.Enqueue(r.UserId);
                    }
                }
                catch
                {
                    // ignored
                }
            }))
            {
                try
                {
                    await Task.Delay(TimeSpan.FromHours(24), CancelToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    
                }
                if (CancelToken.IsCancellationRequested)
                    return;

                _log.Warn("Stopping flower reaction event because it expired.");
                await End();
                
            }
        }
    }
}
