using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Threading;
using NLog;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class CurrencyEvents : NadekoSubmodule
        {
            public enum CurrencyEvent
            {
                FlowerReaction,
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
            private readonly BotConfig _bc;
            private readonly CurrencyService _cs;

            public CurrencyEvents(DiscordSocketClient client, BotConfig bc, CurrencyService cs)
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
                    case CurrencyEvent.FlowerReaction:
                        await FlowerReactionEvent(Context, arg).ConfigureAwait(false);
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
                
                await _client.SetGameAsync($"type {_secretCode} for " + _bc.CurrencyPluralName)
                    .ConfigureAwait(false);
                try
                {
                    var title = GetText("sneakygamestatus_title");
                    var desc = GetText("sneakygamestatus_desc", Format.Bold(100.ToString()) + _bc.CurrencySign, Format.Bold(num.ToString()));
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

            public async Task FlowerReactionEvent(ICommandContext context, int amount)
            {
                if (amount <= 0)
                    amount = 100;

                var title = GetText("flowerreaction_title");
                var desc = GetText("flowerreaction_desc", "🌸", Format.Bold(amount.ToString()) + _bc.CurrencySign);
                var footer = GetText("flowerreaction_footer", 24);
                var msg = await context.Channel.SendConfirmAsync(title,
                        desc, footer: footer)
                    .ConfigureAwait(false);

                await new FlowerReactionEvent(_client, _cs).Start(msg, context, amount);
            }
        }
    }

    public abstract class CurrencyEvent
    {
        public abstract Task Start(IUserMessage msg, ICommandContext channel, int amount);
    }

    public class FlowerReactionEvent : CurrencyEvent
    {
        private readonly ConcurrentHashSet<ulong> _flowerReactionAwardedUsers = new ConcurrentHashSet<ulong>();
        private readonly Logger _log;
        private readonly DiscordSocketClient _client;
        private readonly CurrencyService _cs;

        private IUserMessage StartingMessage { get; set; }

        private CancellationTokenSource Source { get; }
        private CancellationToken CancelToken { get; }

        public FlowerReactionEvent(DiscordSocketClient client, CurrencyService cs)
        {
            _log = LogManager.GetCurrentClassLogger();
            _client = client;
            _cs = cs;
            Source = new CancellationTokenSource();
            CancelToken = Source.Token;
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

        public override async Task Start(IUserMessage umsg, ICommandContext context, int amount)
        {
            StartingMessage = umsg;
            _client.MessageDeleted += MessageDeletedEventHandler;

            try { await StartingMessage.AddReactionAsync(new Emoji("🌸")).ConfigureAwait(false); }
            catch
            {
                try { await StartingMessage.AddReactionAsync(new Emoji("🌸")).ConfigureAwait(false); }
                catch
                {
                    try { await StartingMessage.DeleteAsync().ConfigureAwait(false); }
                    catch { return; }
                }
            }
            using (StartingMessage.OnReaction(_client, async (r) =>
            {
                try
                {
                    if (r.Emote.Name == "🌸" && r.User.IsSpecified && ((DateTime.UtcNow - r.User.Value.CreatedAt).TotalDays > 5) && _flowerReactionAwardedUsers.Add(r.User.Value.Id))
                    {
                        await _cs.AddAsync(r.User.Value, "Flower Reaction Event", amount, false)
                            .ConfigureAwait(false);
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
